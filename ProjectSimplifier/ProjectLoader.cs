using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
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
            var collection = new ProjectCollection(globalProperties);

            ProjectRootElement = new MSBuildProjectRootElement(Microsoft.Build.Construction.ProjectRootElement.Open(projectFilePath, collection, preserveFormatting: true));
            var configurations = DetermineConfigurations(ProjectRootElement);

            Project = new UnconfiguredProject(configurations);
            Project.LoadProjects(collection, globalProperties, projectFilePath);
            Console.WriteLine($"Successfully loaded project file '{projectFilePath}'.");

            var targetProjectProperties = options.TargetProjectProperties.ToImmutableDictionary(p => p.Split('=')[0], p => p.Split('=')[1]);
            SdkBaselineProject = CreateSdkBaselineProject(projectFilePath, Project.FirstConfiguredProject, globalProperties, configurations, targetProjectProperties);
            ProjectRootElement.Reload(throwIfUnsavedChanges: false, preserveFormatting: true);
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
            var imports = project.Imports.Where(i => i.Label != Facts.SharedProjectsImportLabel);
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

            }

            return ProjectStyle.DefaultWithCustomTargets;
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
        private BaselineProject CreateSdkBaselineProject(string projectFilePath, 
                                                         IProject project, 
                                                         ImmutableDictionary<string, string> globalProperties, 
                                                         ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations, 
                                                         ImmutableDictionary<string, string> targetProjectProperties)
        {
            var projectStyle = GetProjectStyle(ProjectRootElement);
            var rootElement = Microsoft.Build.Construction.ProjectRootElement.Open(projectFilePath);

            rootElement.RemoveAllChildren();
            switch (projectStyle)
            {
                case ProjectStyle.Default:
                    rootElement.Sdk = "Microsoft.NET.Sdk";
                    break;
                case ProjectStyle.DefaultWithCustomTargets:
                    var imports = ProjectRootElement.Imports;

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
            propGroup.AddProperty("TargetFramework", project.GetTargetFramework());
            propGroup.AddProperty("OutputType", project.GetPropertyValue("OutputType") ?? throw new InvalidOperationException("OutputType is not set!"));

            var newGlobalProperties = globalProperties.AddRange(targetProjectProperties);
            // Create a new collection because a project with this name has already been loaded into the global collection.
            var pc = new ProjectCollection(newGlobalProperties);
            var newProject = new UnconfiguredProject(configurations);
            newProject.LoadProjects(pc, newGlobalProperties, rootElement);

            // If the original project had the TargetFramework property don't touch it during conversion.
            var propertiesInTheBaseline = ImmutableArray.Create("OutputType");
            if (project.GetProperty("TargetFramework") != null)
            {
                propertiesInTheBaseline = propertiesInTheBaseline.Add("TargetFramework");
            }
            return new BaselineProject(newProject, propertiesInTheBaseline, targetProjectProperties, projectStyle);
        }
    }
}
