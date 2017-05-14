using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MSBuildSdkDiffer
{
    internal struct ItemsDiff
    {
        public readonly string ItemType;
        public readonly ImmutableArray<IProjectItem> DefaultedItems;
        public readonly ImmutableArray<IProjectItem> NotDefaultedItems;
        public readonly ImmutableArray<IProjectItem> IntroducedItems;
        public readonly ImmutableArray<(IProjectItem oldProp, IProjectItem newProp)> ChangedItems;

        public ItemsDiff(string itemType, ImmutableArray<IProjectItem> defaultedItems, ImmutableArray<IProjectItem> notDefaultedItems, ImmutableArray<IProjectItem> introducedItems, ImmutableArray<(IProjectItem, IProjectItem)> changedItems) : this()
        {
            ItemType = itemType;
            DefaultedItems = defaultedItems;
            NotDefaultedItems = notDefaultedItems;
            IntroducedItems = introducedItems;
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
                    changedItems.AddRange(DefaultedItems.Select(s => $"= {s.EvaluatedInclude}"));
                }

                if (!IntroducedItems.IsEmpty)
                {
                    changedItems.AddRange(DefaultedItems.Select(s => $"+ {s.EvaluatedInclude}"));
                }

                lines.AddRange(changedItems.OrderBy(s => s.TrimStart('+', '-', '=', ' ')));
                lines.Add("");
            }

            return lines.ToImmutable();
        }
    }
}