using Facts;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;

namespace MSBuildAbstractions
{
    public class MSBuildWorkspace
    {
        public ImmutableArray<MSBuildWorkspaceItem> WorkspaceItems { get; }

        public MSBuildWorkspace(ImmutableArray<string> paths)
        {
            var items = ImmutableArray.CreateBuilder<MSBuildWorkspaceItem>();

            var globalProperties = ImmutableDictionary<string, string>.Empty;
            using var collection = new ProjectCollection();

            foreach (var path in paths)
            {
                var root = new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
                var configurations = DetermineConfigurations(root);

                var unconfiguredProject = new UnconfiguredProject(configurations);
                unconfiguredProject.LoadProjects(collection, globalProperties, path);

                var props = InitializeTargetProjectProperties();

                var baseline = CreateSdkBaselineProject(path, unconfiguredProject.FirstConfiguredProject, root, globalProperties, configurations, props);
                root.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);

                var rootDirectory = Directory.GetParent(path);

                var item = new MSBuildWorkspaceItem(root, unconfiguredProject, baseline, rootDirectory);
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

        private static ImmutableDictionary<string, string> InitializeTargetProjectProperties(IEnumerable<string> targetProjectProperties = null)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, string>();
            if (targetProjectProperties is object)
            {
                foreach (var item in targetProjectProperties)
                {
                    var parts = item.Split('=');
                    builder.Add(parts[0], parts[1]);
                }
            }

            var props = builder.ToImmutable();
            return props;
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private BaselineProject CreateSdkBaselineProject(string projectFilePath,
                                                         IProject project,
                                                         IProjectRootElement root,
                                                         ImmutableDictionary<string, string> globalProperties,
                                                         ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations,
                                                         ImmutableDictionary<string, string> targetProjectProperties)
        {
            var projectStyle = GetProjectStyle(root);
            var rootElement = ProjectRootElement.Open(projectFilePath);

            rootElement.RemoveAllChildren();
            switch (projectStyle)
            {
                case ProjectStyle.Default:
                    rootElement.Sdk = DesktopFacts.DefaultSDKAttribute;
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

            var newGlobalProperties = globalProperties.AddRange(targetProjectProperties);

            // Create a new collection because a project with this name has already been loaded into the global collection.
            using var pc = new ProjectCollection(newGlobalProperties);
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

            return new BaselineProject(newProject, propertiesInTheBaseline, targetProjectProperties, projectStyle);
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
