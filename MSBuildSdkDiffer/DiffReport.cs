using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    internal class Differ
    {
        private readonly IProject _project;
        private readonly IEnumerable<string> _propertiesInFile;
        private readonly IProject _sdkBaselineProject;

        public Differ(IProject project, IEnumerable<string> propertiesInFile, IProject sdkBaselineProject)
        {
            _project = project ?? throw new System.ArgumentNullException(nameof(project));
            _propertiesInFile = propertiesInFile ?? throw new System.ArgumentNullException(nameof(propertiesInFile));
            _sdkBaselineProject = sdkBaselineProject ?? throw new System.ArgumentNullException(nameof(sdkBaselineProject));
        }

        public PropertiesDiff GetPropertiesDiff()
        {
            var defaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var notDefaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var changedProps = ImmutableArray.CreateBuilder<(IProjectProperty, IProjectProperty)>();

            foreach (var propInFile in _propertiesInFile)
            {
                var originalEvaluatedProp = _project.GetProperty(propInFile);
                var newEvaluatedProp = _sdkBaselineProject.GetProperty(propInFile);
                if (newEvaluatedProp != null)
                {
                    if (originalEvaluatedProp.EvaluatedValue != newEvaluatedProp.EvaluatedValue)
                    {
                        changedProps.Add((originalEvaluatedProp, newEvaluatedProp));
                    }
                    else
                    {
                        defaultedProps.Add(newEvaluatedProp);
                    }
                }
                else
                {
                    notDefaultedProps.Add(originalEvaluatedProp);
                }
            }

            return new PropertiesDiff(defaultedProps.ToImmutable(), notDefaultedProps.ToImmutable(), changedProps.ToImmutable());
        }

        public void GenerateReport(string reportFilePath)
        {
            var report = new List<string>();
            report.AddRange(GetPropertiesDiff().GetDiffLines());
            
            var oldItemGroups = from oldItem in _project.Items group oldItem by oldItem.ItemType;
            var newItemGroups = from newItem in _sdkBaselineProject.Items group newItem by newItem.ItemType;

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
