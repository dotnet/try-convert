using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    internal class ProjectLoader
    {
        public MSBuildProject Project { get; private set; }
        public MSBuildProject SdkBaselineProject { get; private set; }
        public ProjectRootElement ProjectRootElement { get; private set; }

        public void LoadProjects(Options options)
        {
            string projectPath = Path.GetFullPath(options.ProjectFilePath);

            if (!File.Exists(projectPath))
            {
                Console.Error.WriteLine($"The project file '{projectPath}' does not exist or is inaccessible.");
                return;
            }

            Dictionary<string, string> globalProperties = InitializeGlobalProperties(options);

            var collection = new ProjectCollection(globalProperties);
            Project = new MSBuildProject(collection.LoadProject(projectPath));
            Console.WriteLine($"Successfully loaded project file '{projectPath}'.");

            //Stash away names of properties in the file since to create the sdk baseline, we'll modify the project in memory.
            ProjectRootElement = ProjectRootElement.Open(Project.FullPath).DeepClone();
            SdkBaselineProject = CreateSdkBaselineProject(Project, globalProperties);
            Console.WriteLine($"Successfully loaded sdk baseline of project.");
        }

        private static Dictionary<string, string> InitializeGlobalProperties(Options options)
        {
            var globalProperties = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(options.RoslynTargetsPath))
            {
                globalProperties.Add("RoslynTargetsPath", options.RoslynTargetsPath);
            }

            if (!string.IsNullOrEmpty(options.MSBuildSdksPath))
            {
                globalProperties.Add("MSBuildSDKsPath", options.MSBuildSdksPath);
            }

            return globalProperties;
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private static MSBuildProject CreateSdkBaselineProject(MSBuildProject project, IDictionary<string, string> globalProperties)
        {
            var rootElement = ProjectRootElement.Open(project.FullPath);
            rootElement.RemoveAllChildren();
            rootElement.Sdk = "Microsoft.NET.Sdk";
            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty("TargetFramework", project.GetTargetFramework());
            propGroup.AddProperty("OutputType", project.GetPropertyValue("OutputType") ?? throw new InvalidOperationException("OutputType is not set!"));

            // Create a new collection because a project with this name has already been loaded into the global collection.
            var pc = new ProjectCollection(globalProperties);
            var newProjectModel = new MSBuildProject(new Project(rootElement, null, "15.0", pc));
            return newProjectModel;
        }

    }
}
