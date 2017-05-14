using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace MSBuildSdkDiffer
{
    internal class Differ
    {
        private readonly IProject _project;
        private readonly IProject _sdkBaselineProject;

        public Differ(IProject project, IProject sdkBaselineProject)
        {
            _project = project ?? throw new System.ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject ?? throw new System.ArgumentNullException(nameof(sdkBaselineProject));
        }

        public PropertiesDiff GetPropertiesDiff()
        {
            var defaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var notDefaultedProps = ImmutableArray.CreateBuilder<IProjectProperty>();
            var changedProps = ImmutableArray.CreateBuilder<(IProjectProperty, IProjectProperty)>();

            var propertiesInFile = _project.Properties.Where(p => p.IsDefinedInProject).Select(p => p.Name).Distinct();

            foreach (var propInFile in propertiesInFile)
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

        public ImmutableArray<ItemsDiff> GetItemsDiff()
        {
            var oldItemGroups = from oldItem in _project.Items group oldItem by oldItem.ItemType;
            var newItemGroups = from newItem in _sdkBaselineProject.Items group newItem by newItem.ItemType;

            var addedRemovedGroups = from og in oldItemGroups
                                     from ng in newItemGroups
                                     where og.Key == ng.Key
                                     select new {
                                                 ItemType = og.Key,
                                                 DefaultedItems = ng.Intersect(og, ProjectItemComparer.Instance),
                                                 IntroducedItems = ng.Except(og, ProjectItemComparer.Instance),
                                                 NotDefaultedItems = og.Except(ng, ProjectItemComparer.Instance),
                                                };

            var builder = ImmutableArray.CreateBuilder<ItemsDiff>();

            foreach (var group in addedRemovedGroups)
            {
                var defaultedItems = group.DefaultedItems.ToImmutableArray();
                var notDefaultedItems = group.NotDefaultedItems.ToImmutableArray();
                var introducedItems = group.IntroducedItems.ToImmutableArray();

                var diff = new ItemsDiff(group.ItemType, defaultedItems, notDefaultedItems, introducedItems, ImmutableArray<(IProjectItem, IProjectItem)>.Empty);
                builder.Add(diff);
            }

            return builder.ToImmutable();
        }

        public void GenerateReport(string reportFilePath)
        {
            var report = new List<string>();
            report.AddRange(GetPropertiesDiff().GetDiffLines());

            var itemDiffs = GetItemsDiff();
            foreach (var diff in itemDiffs)
            {
                // Items that start with _ are private items. Not much value in reporting them.
                if (diff.ItemType.StartsWith("_"))
                {
                    continue;
                }

                report.AddRange(diff.GetDiffLines());
            }

            File.WriteAllLines(reportFilePath, report);
        }
    }
}
