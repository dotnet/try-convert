using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    internal class DiffReport
    {
        public static void GenerateReport(Project project, List<string> propertiesInFile, Project sdkBaselineProject, string reportFilePath)
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
                    List<string> changedItems = new List<string>();
                    if (removedItems.Any())
                    {
                        changedItems.AddRange(removedItems);
                    }

                    if (addedItems.Any())
                    {
                        changedItems.AddRange(addedItems);
                    }

                    report.AddRange(changedItems.OrderBy(s => s.TrimStart('+', '-', ' ')));
                    report.Add("");
                }
            }

            File.WriteAllLines(reportFilePath, report);
        }
    }
}
