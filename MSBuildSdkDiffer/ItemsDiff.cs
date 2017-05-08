using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    internal struct ItemsDiff
    {
        public readonly string ItemType;
        public readonly ImmutableArray<ProjectItem> DefaultedItems;
        public readonly ImmutableArray<ProjectItem> NotDefaultedItems;
        public readonly ImmutableArray<(ProjectItem oldProp, ProjectItem newProp)> ChangedItems;

        public ItemsDiff(string itemType, ImmutableArray<ProjectItem> defaultedItems, ImmutableArray<ProjectItem> notDefaultedItems, ImmutableArray<(ProjectItem, ProjectItem)> changedItems) : this()
        {
            ItemType = itemType;
            DefaultedItems = defaultedItems;
            NotDefaultedItems = notDefaultedItems;
            ChangedItems = changedItems;
        }

        public ImmutableArray<string> GetDiffLines()
        {
            var lines = ImmutableArray.CreateBuilder<string>();

            if (!DefaultedItems.IsEmpty && !NotDefaultedItems.IsEmpty)
            {
                lines.Add($"{ ItemType} items:");
                List<string> changedItems = new List<string>();
                if (!NotDefaultedItems.IsEmpty)
                {
                    changedItems.AddRange(NotDefaultedItems.Select(s => $"- {s.EvaluatedInclude}"));
                }

                if (!DefaultedItems.IsEmpty)
                {
                    changedItems.AddRange(DefaultedItems.Select(s => $"+ {s.EvaluatedInclude}"));
                }

                lines.AddRange(changedItems.OrderBy(s => s.TrimStart('+', '-', ' ')));
                lines.Add("");
            }

            return lines.ToImmutable();
        }
    }
}