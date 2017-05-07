using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using CommandLine;

namespace MSBuildSdkDiffer
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args);
            switch (options)
            {
                case Parsed<Options> parsedOptions:
                    return Run(parsedOptions.Value);
                case NotParsed<Options> notParsed:
                    foreach(var error in notParsed.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return 1;
            }
            return 1;
        }

        private static int Run(Options options)
        {
            string projectPath = Path.GetFullPath(options.ProjectFilePath);

            if (!File.Exists(projectPath))
            {
                Console.Error.WriteLine($"The project file '{projectPath}' does not exist or is inaccessible.");
                return 2;
            }

            Dictionary<string, string> globalProperties = InitializeGlobalProperties(options);

            var collection = new ProjectCollection(globalProperties);
            MSBuildProject project = new MSBuildProject(collection.LoadProject(projectPath));
            
            Console.WriteLine($"Successfully loaded project file '{projectPath}'.");

            //Stash away names of properties in the file since to create the sdk baseline, we'll modify the project in memory.
            var rootElement = ProjectRootElement.Open(projectPath);
            var propertiesInFile = rootElement.Properties.Select(p => p.Name).Distinct().ToList();
            MSBuildProject sdkBaselineProject = CreateSdkBaselineProject(project, rootElement, globalProperties);
            Console.WriteLine($"Successfully loaded sdk baseline of project.");

            project.LogProjectProperties("currentProject.log");
            sdkBaselineProject.LogProjectProperties("sdkBaseLineProject.log");
            var differ = new Differ(project, propertiesInFile, sdkBaselineProject);
            differ.GenerateReport("report.diff");

            return 0;
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
        private static MSBuildProject CreateSdkBaselineProject(MSBuildProject project, ProjectRootElement rootElement, IDictionary<string, string> globalProperties)
        {
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