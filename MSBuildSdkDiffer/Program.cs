using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuildDiffer
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Please specify the full path to an MSBuild project to load.");
                return 1;
            }

            string projectPath = Path.GetFullPath(args[0]);

            if (!File.Exists(projectPath))
            {
                Console.Error.WriteLine($"The project file '{projectPath}' does not exist or is inaccessible.");
                return 2;
            }

            Project project = new Project(projectPath);
            Console.WriteLine($"Successfully loaded project file '{projectPath}'.");

            //Stash away names of properties in the file since to create the sdk baseline, we'll modify the project in memory.
            var rootElement = ProjectRootElement.Open(projectPath);
            var propertiesInFile = rootElement.Properties.Select(p => p.Name).Distinct().ToList();
            Project sdkBaselineProject = CreateSdkBaselineProject(project, rootElement);
            Console.WriteLine($"Successfully loaded sdk baseline of project.");

            LogProjectProperties(project, "currentProject.log");
            LogProjectProperties(sdkBaselineProject, "sdkBaseLineProject.log");
            GenerateReport(project, propertiesInFile, sdkBaselineProject, "report.diff");

            return 0;
        }

        private static void GenerateReport(Project project, List<string> propertiesInFile, Project sdkBaselineProject, string reportFilePath)
        {
            var report = new List<string>();
            var defaultedProps = new List<string>();
            var notDefaultedProps = new List<string>();
            var changedProps = new List<string>();
            foreach (var propInFile in propertiesInFile)
            {
                var originalEvaluatedProp = project.GetProperty(propInFile);
                var newEvaluatedProp = sdkBaselineProject.GetProperty(propInFile);
                var originalProp = $"- {originalEvaluatedProp.Name} = {originalEvaluatedProp.EvaluatedValue}";
                if (newEvaluatedProp != null)
                {
                    var newProp = $"+ {newEvaluatedProp.Name} = {newEvaluatedProp.EvaluatedValue}";
                    if (originalEvaluatedProp.EvaluatedValue != newEvaluatedProp.EvaluatedValue)
                    {
                        changedProps.Add(originalProp);
                        changedProps.Add(newProp);
                    }
                    else
                    {
                        defaultedProps.Add(newProp);
                    }
                }
                else
                {
                    notDefaultedProps.Add(originalProp);
                }
            }

            if (defaultedProps.Any())
            {
                report.Add("Properties that are defaulted by the SDK:");
                report.AddRange(defaultedProps);
                report.Add("");
            }
            if (notDefaultedProps.Any())
            {
                report.Add("Properties that are not defaulted by the SDK:");
                report.AddRange(notDefaultedProps);
                report.Add("");
            }
            if (changedProps.Any())
            {
                report.Add("Properties whose value is different from the SDK's default:");
                report.AddRange(changedProps);
                report.Add("");
            }

            var oldItemGroups = from oldItem in project.Items group oldItem by oldItem.ItemType;
            var newItemGroups = from newItem in sdkBaselineProject.Items group newItem by newItem.ItemType;

            var addedRemovedGroups = from og in oldItemGroups
                                     from ng in newItemGroups
                                     where og.Key == ng.Key
                                     select new { ItemType = og.Key, AddedItems = ng.Except(og, ProjectItemComparer.Instance), RemovedItems = og.Except(ng, ProjectItemComparer.Instance) };

            foreach (var group in addedRemovedGroups)
            {
                // Items that start with _ are private items. Not much value in reporting them.
                if (group.ItemType.StartsWith("_"))
                {
                    continue;
                }

                var addedItems = group.AddedItems.Select(s => $"+ {s.EvaluatedInclude}");
                var removedItems = group.RemovedItems.Select(s => $"- {s.EvaluatedInclude}");

                if (addedItems.Any() || removedItems.Any())
                {
                    report.Add($"{ group.ItemType} items:");
                    if (removedItems.Any())
                    {
                        report.AddRange(removedItems);
                    }

                    if (addedItems.Any())
                    {
                        report.AddRange(addedItems);
                    }

                    report.Add("");
                }
            }

            File.WriteAllLines(reportFilePath, report);
        }

        private static void LogProjectProperties(Project project, string logFileName)
        {
            var lines = new List<string>();
            foreach (var prop in project.Properties.OrderBy(p => p.Name))
            {
                lines.Add($"{prop.Name} = {prop.EvaluatedValue}");
            }
            File.WriteAllLines(logFileName, lines);
        }

        /// <summary>
        /// Clear out the project's construction model and add a simple SDK-based project to get a baseline.
        /// We need to use the same name as the original csproj and same path so that all the default that derive
        /// from name\path get the right values (there are a lot of them).
        /// </summary>
        private static Project CreateSdkBaselineProject(Project project, ProjectRootElement rootElement)
        {
            rootElement.RemoveAllChildren();
            rootElement.Sdk = "Microsoft.NET.Sdk";
            var propGroup = rootElement.AddPropertyGroup();
            propGroup.AddProperty("TargetFramework", GetTargetFramework(project));
            propGroup.AddProperty("OutputType", project.GetPropertyValue("OutputType") ?? throw new InvalidOperationException("OutputType is not set!"));

            // Create a new collection because a project with this name has already been loaded into the global collection.
            var pc = new ProjectCollection(ToolsetDefinitionLocations.Default);
            var newProjectModel = new Project(rootElement, null, "15.0", pc);
            return newProjectModel;
        }

        private static string GetTargetFramework(Project project)
        {
            var tf = project.GetPropertyValue("TargetFramework");
            if (tf != "")
            {
                return tf;
            }

            var tfi = project.GetPropertyValue("TargetFrameworkIdentifier");
            if (tfi == "")
            {
                throw new InvalidOperationException("TargetFrameworkIdentifier is not set!");
            }

            switch (tfi)
            {
                case ".NETFramework": tf = "net";
                    break;
                case ".NETStandard": tf = "netstandard";
                    break;
                case ".NETCoreApp": tf = "netcoreapp";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown TargetFrameworkIdentifier {tfi}");
            }

            var tfv = project.GetPropertyValue("TargetFrameworkVersion");
            if (tfv == "")
            {
                throw new InvalidOperationException("TargetFrameworkVersion is not set!");
            }

            tf += tfv.TrimStart('v');

            return tf;
        }
    }
}