using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace ProjectSimplifier
{
    internal class ProjectLoader
    {
        public UnconfiguredProject Project { get; private set; }
        public BaselineProject SdkBaselineProject { get; private set; }
        public IProjectRootElement ProjectRootElement { get; private set; }
        
        public void LoadProjects(Options options)
        {
            string projectFilePath = Path.GetFullPath(options.ProjectFilePath);

            if (!File.Exists(projectFilePath))
            {
                Console.Error.WriteLine($"The project file '{projectFilePath}' does not exist or is inaccessible.");
                return;
            }

            ImmutableDictionary<string, string> globalProperties = InitializeGlobalProperties(options);
            var collection = new ProjectCollection(globalProperties, loggers: null, toolsetDefinitionLocations: ToolsetDefinitionLocations.Local);

            ProjectRootElement = new MSBuildProjectRootElement(Microsoft.Build.Construction.ProjectRootElement.Open(projectFilePath).DeepClone());
            var configurations = DetermineConfigurations(ProjectRootElement);

            Project = new UnconfiguredProject(configurations);
            Project.LoadProjects(collection, globalProperties, projectFilePath);
            Console.WriteLine($"Successfully loaded project file '{projectFilePath}'.");

            SdkBaselineProject = CreateSdkBaselineProject(projectFilePath, Project.FirstConfiguredProject, globalProperties, configurations);
            Console.WriteLine($"Successfully loaded sdk baseline of project.");
        }

        private ImmutableDictionary<string, ImmutableDictionary<string, string>> DetermineConfigurations(IProjectRootElement projectRootElement)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>>();
            foreach (var propertyGroup in projectRootElement.PropertyGroups)
            {
                if (MSBuildUtilities.ConditionToDimensionValues(propertyGroup.Condition, out var dimensionValues))
                {
                    var name = MSBuildUtilities.GetConfigurationName(dimensionValues);
                    builder.Add(name, dimensionValues.ToImmutableDictionary());
                }
            }

            return builder.ToImmutable();
        }

        public static ProjectStyle GetProjectStyle(IProjectRootElement project)
        {
            if (project.ImportGroups.Any())
            {
                return ProjectStyle.Custom;
            }

            // Exclude shared project references since they show up as imports.
            var imports = project.Imports.Where(i => i.Label != "Shared");
            if (imports.Count() == 2)
            {
                var firstImport = project.Imports.First();
                var lastImport = project.Imports.Last();

                var firstImportFileName = Path.GetFileName(firstImport.Project);
                var lastImportFileName = Path.GetFileName(lastImport.Project);

                if (Facts.PropsConvertibleToSDK.Contains(firstImportFileName, StringComparer.OrdinalIgnoreCase) && 
                    Facts.TargetsConvertibleToSDK.Contains(lastImportFileName, StringComparer.OrdinalIgnoreCase))
                {
                    return ProjectStyle.Default;
                }

                return ProjectStyle.DefaultWithCustomTargets;
            }

            return ProjectStyle.Custom;
        }

        private static ImmutableDictionary<string, string> InitializeGlobalProperties(Options options)
        {
            var globalProperties = ImmutableDictionary.CreateBuilder<string, string>();
            if (!string.IsNullOrEmpty(options.RoslynTargetsPath))
            {
                globalProperties.Add("RoslynTargetsPath", options.RoslynTargetsPath);
            }

            if (!string.IsNullOrEmpty(options.MSBuildSdksPath))
            {
                globalProperties.Add("MSBuildSDKsPath", options.MSBuildSdksPath);
            }

            return globalProperties.ToImmutable();
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private BaselineProject CreateSdkBaselineProject(string projectFilePath, IProject project, ImmutableDictionary<string, string> globalProperties, ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations)
        {
            var rootElement = Microsoft.Build.Construction.ProjectRootElement.Open(projectFilePath);
            rootElement.RemoveAllChildren();
            rootElement.Sdk = "Microsoft.NET.Sdk";
            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty("TargetFramework", project.GetTargetFramework());
            propGroup.AddProperty("OutputType", project.GetPropertyValue("OutputType") ?? throw new InvalidOperationException("OutputType is not set!"));

            // Create a new collection because a project with this name has already been loaded into the global collection.
            var pc = new ProjectCollection(globalProperties);
            var newProject = new UnconfiguredProject(configurations);
            newProject.LoadProjects(pc, globalProperties, rootElement);
            return new BaselineProject(newProject, ImmutableArray.Create("OutputType"), GetProjectStyle(ProjectRootElement));
        }

    }
}
