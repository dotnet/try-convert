using Facts;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace MSBuild.Abstractions
{
    public class MSBuildWorkspace
    {
        public ImmutableArray<MSBuildWorkspaceItem> WorkspaceItems { get; }

        public MSBuildWorkspace(ImmutableArray<string> paths, bool noBackup)
        {
            var items = ImmutableArray.CreateBuilder<MSBuildWorkspaceItem>();

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
                        File.Copy(path, path + ".old");
                    }

                    // Let them know about System.Web
                    if (MSBuildHelpers.IsProjectReferencingSystemWeb(root))
                    {
                        Console.WriteLine($"'{root.FullPath}' references System.Web, which is unsupported on .NET Core. You may have significant work remaining after conversion.");
                    }

                    var configurations = DetermineConfigurations(root);

                    var unconfiguredProject = new UnconfiguredProject(configurations);
                    unconfiguredProject.LoadProjects(collection, globalProperties, path);

                    var baseline = CreateSdkBaselineProject(path, unconfiguredProject.FirstConfiguredProject, root, configurations);
                    root.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);

                    var item = new MSBuildWorkspaceItem(root, unconfiguredProject, baseline);
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
            var rootElement = ProjectRootElement.Open(projectFilePath);

            rootElement.RemoveAllChildren();
            switch (projectStyle)
            {
                case ProjectStyle.Default:
                case ProjectStyle.DefaultSubset:
                    rootElement.Sdk = MSBuildFacts.DefaultSDKAttribute;
                    break;
                case ProjectStyle.WindowsDesktop:
                    rootElement.Sdk = DesktopFacts.WinSDKAttribute;
                    break;
                case ProjectStyle.DefaultWithCustomTargets:
                    var imports = root.Imports;

                    void CopyImport(ProjectImportElement import)
                    {
                        var newImport = rootElement.AddImport(import.Project);
                        newImport.Condition = import.Condition;
                    }
                    CopyImport(imports.First());
                    CopyImport(imports.Last());
                    break;
                default:
                    throw new NotSupportedException("This project has custom imports in a manner that's not supported.");
            }

            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty(MSBuildFacts.TargetFrameworkNodeName, project.GetTargetFramework());
            propGroup.AddProperty(MSBuildFacts.OutputTypeNodeName, project.GetPropertyValue(MSBuildFacts.OutputTypeNodeName) ?? throw new InvalidOperationException("OutputType is not set!"));

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

            return new BaselineProject(newProject, propertiesInTheBaseline, projectStyle);
        }

        private ProjectStyle GetProjectStyle(IProjectRootElement project)
        {
            if (project.ImportGroups.Any())
            {
                return ProjectStyle.Custom;
            }

            // Exclude shared project references since they show up as imports.
            var imports = project.Imports.Where(i => i.Label != MSBuildFacts.SharedProjectsImportLabel);
            var importsCount = imports.Count();

            if (importsCount > 0)
            {
                var firstImport = project.Imports.First();
                var firstImportFileName = Path.GetFileName(firstImport.Project);

                if (importsCount == 1 && MSBuildFacts.TargetsConvertibleToSDK.Contains(firstImportFileName, StringComparer.OrdinalIgnoreCase))
                {
                    return ProjectStyle.DefaultSubset;
                }
                else
                {

                    var lastImport = project.Imports.Last();
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
                        if (MSBuildHelpers.IsWPF(project) || MSBuildHelpers.IsWinForms(project) || MSBuildHelpers.IsDesktop(project))
                        {
                            return ProjectStyle.WindowsDesktop;
                        }
                        return ProjectStyle.Default;
                    }
                }
            }
            else
            {
                return ProjectStyle.Custom;
            }

            return ProjectStyle.DefaultWithCustomTargets;
        }

        private bool IsSupportedProjectType(MSBuildProjectRootElement root)
        {
            if (root.Sdk.ContainsIgnoreCase(MSBuildFacts.DefaultSDKAttribute))
            {
                Console.WriteLine($"'{root.FullPath}' is already a .NET SDK-style project, so it won't be converted.");
                return false;
            }

            if (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsSupportedOutputType)))
            {
                Console.WriteLine($"{root.FullPath} does not have a supported OutputType.");
                return false;
            }

            if (MSBuildHelpers.IsDesktop(root)
                && MSBuildHelpers.HasProjectTypeGuidsNode(root)
                && (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreDesktopProjectTypeGuids))))
            {
                var allSupportedProjectTypeGuids = DesktopFacts.KnownSupportedDesktopProjectTypeGuids.Select(ptg => ptg.ToString());
                var allReadProjectTypeGuids = MSBuildHelpers.GetAllProjectTypeGuids(root);

                Console.WriteLine($"{root.FullPath} is an unsupported project type. Not all project type guids are supported.");

                PrintGuidMessage(allSupportedProjectTypeGuids, allReadProjectTypeGuids);

                return false;
            }

            if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsLegacyWebProjectTypeGuidsProperty)))
            {
                Console.WriteLine($"'{root.FullPath}' is a legacy web project, which is unsupported by this tool.");
                return false;
            }

            if (MSBuildHelpers.HasProjectTypeGuidsNode(root)
                && !MSBuildHelpers.IsDesktop(root)
                && (!root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.AllProjectTypeGuidsAreLanguageProjectTypeGuids))))
            {
                Console.WriteLine($"{root.FullPath} is an unsupported project type.");
                return false;
            }

            // It's supported
            return true;

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
        }
    }
}
