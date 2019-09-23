using Facts;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace MSBuildAbstractions
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
                if (!noBackup)
                {
                    File.Copy(path, path + ".old");
                }

                var root = new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
                if (root.Sdk.ContainsIgnoreCase(MSBuildFacts.DefaultSDKAttribute))
                {
                    Console.WriteLine($"'{path}' is already a .NET SDK-style project, so it won't be converted.");
                    continue;
                }

                if (root.PropertyGroups.Any(pg => pg.Properties.Any(ProjectPropertyHelpers.IsLegacyWebProjectTypeGuidsProperty)))
                {
                    Console.WriteLine($"'{path}' is a legacy web project, which is unsupported by this tool.");
                    continue;
                }

                var configurations = DetermineConfigurations(root);

                var unconfiguredProject = new UnconfiguredProject(configurations);
                unconfiguredProject.LoadProjects(collection, globalProperties, path);

                var baseline = CreateSdkBaselineProject(path, unconfiguredProject.FirstConfiguredProject, root, configurations);
                root.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);

                var item = new MSBuildWorkspaceItem(root, unconfiguredProject, baseline);
                items.Add(item);
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
            }

            // Create a new collection because a project with this name has already been loaded into the global collection.
            using var pc = new ProjectCollection();
            var newProject = new UnconfiguredProject(configurations);
            newProject.LoadProjects(pc, rootElement);

            // If the original project had the TargetFramework property don't touch it during conversion.
            var propertiesInTheBaseline = ImmutableArray.Create(MSBuildFacts.OutputTypeNodeName);

            if (project.GetProperty(MSBuildFacts.TargetFrameworkNodeName) is object)
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(MSBuildFacts.TargetFrameworkNodeName);
            }

            if (project.GetProperty(DesktopFacts.UseWinFormsPropertyName) is object)
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add(DesktopFacts.UseWinFormsPropertyName);
            }

            if (project.GetProperty(DesktopFacts.UseWPFPropertyName) is object)
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
            if (imports.Count() == 2)
            {
                var firstImport = project.Imports.First();
                var lastImport = project.Imports.Last();

                var firstImportFileName = Path.GetFileName(firstImport.Project);
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
                    if (MSBuildHelpers.IsWPF(project) || MSBuildHelpers.IsWinForms(project))
                    {
                        return ProjectStyle.WindowsDesktop;
                    }
                    return ProjectStyle.Default;
                }
            }

            return ProjectStyle.DefaultWithCustomTargets;
        }
    }
}
