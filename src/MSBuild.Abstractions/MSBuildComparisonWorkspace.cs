using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    public class MSBuildConversionWorkspace
    {
        public ImmutableArray<MSBuildConversionWorkspaceItem> WorkspaceItems { get; }

        public MSBuildConversionWorkspace(ImmutableArray<string> paths, bool noBackup)
        {
            var items = ImmutableArray.CreateBuilder<MSBuildConversionWorkspaceItem>();

            var globalProperties = ImmutableDictionary<string, string>.Empty;
            using var collection = new ProjectCollection();

            foreach (var path in paths)
            {
                var fileExtension = Path.GetExtension(path);
                if ((StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".fsproj") != 0) &&
                    (StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".csproj") != 0) &&
                    (StringComparer.OrdinalIgnoreCase.Compare(fileExtension, ".vbproj") != 0))
                {
                    Console.WriteLine($"'{path}' is not a .NET project, skipping it.");
                    continue;
                }

                var root = new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
                if (IsSupportedProjectType(root))
                {
                    if (!noBackup)
                    {
                        // Since git doesn't track the new '.old' addition in your changeset,
                        // failing to overwrite will crash the tool if you have one in your directory.
                        // This can be common if you're using the tool a few times and forget to delete the backup.
                        File.Copy(path, path + ".old", overwrite: true);
                    }

                    var configurations = DetermineConfigurations(root);

                    var unconfiguredProject = new UnconfiguredProject(configurations);
                    unconfiguredProject.LoadProjects(collection, globalProperties, path);

                    var baseline = CreateSdkBaselineProject(path, unconfiguredProject.FirstConfiguredProject, root, configurations);
                    root.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);

                    var item = new MSBuildConversionWorkspaceItem(root, unconfiguredProject, baseline);
                    items.Add(item);
                }
            }

            WorkspaceItems = items.ToImmutable();
        }

        public ImmutableDictionary<string, ImmutableDictionary<string, string>> DetermineConfigurations(IProjectRootElement projectRootElement)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>();
            foreach (var propertyGroup in projectRootElement.PropertyGroups)
            {
                if (MSBuildHelpers.ConditionToDimensionValues(propertyGroup.Condition, out var dimensionValues))
                {
                    var name = MSBuildHelpers.GetConfigurationName(dimensionValues);
                    builder.Add(name, dimensionValues.ToImmutableDictionary());

                    // Include $Configuration only and $Platform only conditions
                    foreach (var dimensionValuePair in dimensionValues)
                    {
                        if (!builder.ContainsKey(dimensionValuePair.Value))
                        {
                            var dimensionValueDictionary = new Dictionary<string, string> { { dimensionValuePair.Key, dimensionValuePair.Value } };
                            builder.Add(dimensionValuePair.Value, dimensionValueDictionary.ToImmutableDictionary());
                        }
                    }
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private BaselineProject CreateSdkBaselineProject(string projectFilePath, IProject project, IProjectRootElement root, ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations)
        {
            var projectStyle = GetProjectStyle(root);
            var outputType = GetProjectOutputType(root);
            var rootElement = ProjectRootElement.Open(projectFilePath);

            rootElement.RemoveAllChildren();
            switch (projectStyle)
            {
                case ProjectStyle.Default:
                case ProjectStyle.DefaultSubset:
                case ProjectStyle.MSTest:
                    rootElement.Sdk = MSBuildFacts.DefaultSDKAttribute;
                    break;
                case ProjectStyle.WindowsDesktop:
                    rootElement.Sdk = DesktopFacts.WinSDKAttribute;
                    break;
                default:
                    throw new NotSupportedException($"This project has custom imports in a manner that's not supported. '{projectFilePath}'");
            }

            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty(MSBuildFacts.TargetFrameworkNodeName, project.GetTargetFramework());
            propGroup.AddProperty(MSBuildFacts.OutputTypeNodeName,
                project.GetPropertyValue(MSBuildFacts.OutputTypeNodeName) ?? throw new InvalidOperationException($"OutputType is not set! '{projectFilePath}'"));

            if (projectStyle == ProjectStyle.WindowsDesktop)
            {
                if (MSBuildHelpers.IsWinForms(root))
                {
                    MSBuildHelpers.AddUseWinForms(propGroup);
                }

                if (MSBuildHelpers.IsWPF(root))
                {
                    MSBuildHelpers.AddUseWPF(propGroup);
                }

                // User is referencing WindowsBase only
                if (MSBuildHelpers.IsDesktop(root) && !MSBuildHelpers.HasWPFOrWinForms(propGroup))
                {
                    MSBuildHelpers.AddUseWinForms(propGroup);
                }
            }

            // Create a new collection because a project with this name has already been loaded into the global collection.
            using var pc = new ProjectCollection();
            var newProject = new UnconfiguredProject(configurations);
            newProject.LoadProjects(pc, rootElement);

            // If the original project had the TargetFramework property don't touch it during conversion.
            var propertiesInTheBaseline = ImmutableArray.Create(MSBuildFacts.OutputTypeNodeName);

            if (project.GetProperty(MSBuildFacts.TargetFrameworkNodeName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(MSBuildFacts.TargetFrameworkNodeName);
            }

            if (project.GetProperty(DesktopFacts.UseWinFormsPropertyName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(DesktopFacts.UseWinFormsPropertyName);
            }

            if (project.GetProperty(DesktopFacts.UseWPFPropertyName) is { })
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(DesktopFacts.UseWPFPropertyName);
            }

            return new BaselineProject(newProject, propertiesInTheBaseline, projectStyle, outputType);
        }

        private bool IsSupportedOutputType(ProjectOutputType type) =>
            type switch
            {
                ProjectOutputType.Exe => true,
                ProjectOutputType.Library => true,
                ProjectOutputType.WinExe => true,
                _ => false
            };

        private ProjectOutputType GetProjectOutputType(IProjectRootElement root)
        {
            var outputTypeNode = root.GetOutputTypeNode();
            if (outputTypeNode is null)
            {
                Console.WriteLine($"No OutputType found in the project file '{root.FullPath}'. Are you sure your project builds today?");
                return ProjectOutputType.None;
            }
            else if (ProjectPropertyHelpers.IsLibraryOutputType(outputTypeNode))
            {
                return ProjectOutputType.Library;
            }
            else if (ProjectPropertyHelpers.IsExeOutputType(outputTypeNode))
            {
                return ProjectOutputType.Exe;
            }
            else if (ProjectPropertyHelpers.IsWinExeOutputType(outputTypeNode))
            {
                return ProjectOutputType.WinExe;
            }
            else
            {
                return ProjectOutputType.Other;
            }
        }

        private ProjectStyle GetProjectStyle(IProjectRootElement projectRootElement)
        {
            if (projectRootElement.ImportGroups.Any())
            {
                return ProjectStyle.Custom;
            }

            // Exclude shared project references since they show up as imports.
            // Also exclude any imports that a Nuget package could have brought alone.
            var imports = projectRootElement.Imports.Where(i => i.Label != MSBuildFacts.SharedProjectsImportLabel && !MSBuildHelpers.IsTargetFromNuGetPackage(i));
            var importsCount = imports.Count();

            if (importsCount <= 0)
            {
                return ProjectStyle.Custom;
            }
            else
            {
                var firstImport = imports.First();
                var firstImportFileName = Path.GetFileName(firstImport.Project);

                if (importsCount == 1 && MSBuildFacts.TargetsConvertibleToSDK.Contains(firstImportFileName, StringComparer.OrdinalIgnoreCase))
                {
                    return ProjectStyle.DefaultSubset;
                }
                else
                {
                    var lastImport = imports.Last();
                    var lastImportFileName = Path.GetFileName(lastImport.Project);

                    if (firstImportFileName == FSharpFacts.FSharpTargetsPathVariableName)
                    {
                        firstImportFileName = Path.GetFileName(FSharpFacts.FSharpTargetsPath);
                    }

                    if (lastImportFileName == FSharpFacts.FSharpTargetsPathVariableName)
                    {
                        lastImportFileName = Path.GetFileName(FSharpFacts.FSharpTargetsPath);
                    }

                    if (MSBuildFacts.PropsConvertibleToSDK.Contains(firstImportFileName, StringComparer.OrdinalIgnoreCase) &&
                        MSBuildFacts.TargetsConvertibleToSDK.Contains(lastImportFileName, StringComparer.OrdinalIgnoreCase))
                    {
                        if (MSBuildHelpers.IsNETFrameworkMSTestProject(projectRootElement))
                        {
                            return ProjectStyle.MSTest;
                        }
                        else if (MSBuildHelpers.IsWPF(projectRootElement) || MSBuildHelpers.IsWinForms(projectRootElement) || MSBuildHelpers.IsDesktop(projectRootElement))
                        {
                            return ProjectStyle.WindowsDesktop;
                        }
                        else
                        {
                            return ProjectStyle.Default;
                        }
                    }
                    else
                    {
                        // It's something else, no idea what though
                        return ProjectStyle.Custom;
                    }
                }
            }
        }

        private bool IsSupportedProjectType(IProjectRootElement root)
        {
            if (root.Sdk.ContainsIgnoreCase(MSBuildFacts.DefaultSDKAttribute))
            {
                Console.WriteLine($"'{root.FullPath}' is already a .NET SDK-style project, so it won't be converted.");
                return false;
            }

            if (!IsSupportedOutputType(GetProjectOutputType(root)))
            {
                Console.WriteLine($"{root.FullPath} does not have a supported OutputType.");
                return false;
            }

            if (root.ItemGroups.Any(ig => ig.Items.Any(ProjectItemHelpers.IsReferencingSystemWeb)))
            {
                Console.WriteLine($"{root.FullPath} contains a reference to System.Web, which is not supported on .NET Core. You may have significant work ahead of you to fully port this project.");
            }

            // Lots of wild old project types have project type guids that the old project system uses to light things up!
            // Also some references that are incompatible.
            var projectType = GetProjectSupportType(root);
            switch (projectType)
            {
                case ProjectSupportType.LegacyWeb:
                    Console.WriteLine($"'{root.FullPath}' is a legacy web project and/or reference System.Web. Legacy Web projects and System.Web are unsupported on .NET Core. You will need to rewrite your application or find a way to not depend on System.Web to convert this project.");
                    return false;
                case ProjectSupportType.CodedUITest:
                    Console.WriteLine($"'{root.FullPath}' is a coded UI test. Coded UI tests are deprecated and not convertable to .NET Core.");
                    return false;
                case ProjectSupportType.UnknownTestProject:
                    Console.WriteLine($"'{root.FullPath}' has invalid Project Type Guids for test projects and is not supported.");
                    return false;
                case ProjectSupportType.UnsupportedTestType:
                    Console.WriteLine($"'{root.FullPath}' is an unsupported MSTest test type. Only Unit Tests are supported.");
                    return false;
                case ProjectSupportType.Desktop:
                case ProjectSupportType.MSTest:
                    return true;
                case ProjectSupportType.Unsupported:
                default:
                    if (MSBuildHelpers.HasProjectTypeGuidsNode(root))
                    {
                        var allSupportedProjectTypeGuids = DesktopFacts.KnownSupportedDesktopProjectTypeGuids.Select(ptg => ptg.ToString());
                        var allReadProjectTypeGuids = MSBuildHelpers.GetAllProjectTypeGuids(root);
                        Console.WriteLine($"{root.FullPath} is an unsupported project type. Not all project type guids are supported.");
                        PrintGuidMessage(allSupportedProjectTypeGuids, allReadProjectTypeGuids);
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

            static void PrintGuidMessage(IEnumerable<string> allSupportedProjectTypeGuids, IEnumerable<string> allReadProjectTypeGuids)
            {
                Console.WriteLine("All supported project type guids:");
                foreach (var guid in allSupportedProjectTypeGuids)
                {
                    Console.WriteLine($"\t{guid}");
                }

                Console.WriteLine("All given project type guids:");
                foreach (var guid in allReadProjectTypeGuids)
                {
                    Console.WriteLine($"\t{guid}");
                }
            }

            static ProjectSupportType GetProjectSupportType(IProjectRootElement root)
            {
                if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsLegacyWebProjectTypeGuidsProperty)))
                {
                    return ProjectSupportType.LegacyWeb;
                }
                else if (MSBuildHelpers.IsNETFrameworkMSTestProject(root))
                {
                    if (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreLegacyTestProjectTypeGuids)))
                    {
                        return ProjectSupportType.UnknownTestProject;
                    }

                    if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsCodedUITest)))
                    {
                        return ProjectSupportType.CodedUITest;
                    }

                    if (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsUnitTestType)))
                    {
                        return ProjectSupportType.UnsupportedTestType;
                    }

                    return ProjectSupportType.MSTest;
                }
                if (MSBuildHelpers.IsDesktop(root) &&
                    !root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreDesktopProjectTypeGuids)))
                {
                    return ProjectSupportType.Unsupported;
                }

                return ProjectSupportType.Desktop;
            }
        }

        private enum ProjectSupportType
        {
            Desktop,
            LegacyWeb,
            MSTest,
            Unsupported,
            CodedUITest,
            UnsupportedTestType,
            UnknownTestProject,
        }
    }
}
