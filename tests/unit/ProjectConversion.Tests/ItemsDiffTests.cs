using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Conversion;
using MSBuild.Abstractions.Tests.Mocks;
using Xunit;

namespace MSBuild.Abstractions.Tests
{
    public class ItemsDiffTests
    {
        [Theory]
        [InlineData("Compile:a.cs,b.cs", "Compile:a.cs", "Compile:a.cs", "Compile:b.cs", null, null)]
        [InlineData("Compile:a.cs,b.cs", "Compile:e.cs,d.cs", null, "Compile:a.cs,b.cs", "Compile:e.cs,d.cs", null)]
        [InlineData("Compile:a.cs,b.cs", "Compile:a.cs,b.cs,e.cs", "Compile:a.cs,b.cs", null, "Compile:e.cs", null)]
        [InlineData("Compile:a.cs,b.cs", "Compile:a.cs,d.cs", "Compile:a.cs", "Compile:b.cs", "Compile:d.cs", null)]
        [InlineData("Compile:a.cs,b.cs;None:a.xml", "Compile:a.cs", "Compile:a.cs", "Compile:b.cs;None:a.xml", null, null)]
        [InlineData("Compile:a.cs,b.cs;None:a.cs", "Compile:a.cs", "Compile:a.cs", "Compile:b.cs;None:a.cs", null, null)]
        [InlineData("Compile:a.cs|x=y,b.cs|x=y", "Compile:a.cs", null, "Compile:b.cs", null, "Compile:a.cs")]
        [InlineData("Compile:a.cs|x=y|z=z,b.cs|x=y", "Compile:a.cs|x=y", null, "Compile:b.cs", null, "Compile:a.cs")]
        [InlineData("Compile:a.cs|x=y,b.cs|x=y", "Compile:a.cs|x=z", null, "Compile:b.cs", null, "Compile:a.cs")]
        public void ItemsDiff(string projectItems, string sdkBaselineItems, string expectedDefaultedItems, string expectedNotDefaultedItems, string expectedIntroducedItems, string expectedChangedItems)
        {
            var project = IProjectFactory.Create(GetItems(projectItems));
            var sdkBaselineProject = IProjectFactory.Create(GetItems(sdkBaselineItems));

            var differ = new Differ(project, sdkBaselineProject);

            var diffs = differ.GetItemsDiff();

            if (expectedDefaultedItems == null)
            {
                Assert.All(diffs, diff => Assert.Empty(diff.DefaultedItems));
            }
            else
            {
                var expectedDiffItems = GetItems(expectedDefaultedItems);
                var matchingItems = diffs.Select(diff => (diff.DefaultedItems.Select(i => i.EvaluatedInclude), expectedDiffItems.SingleOrDefault(d => d.ItemType == diff.ItemType).Items));
                Assert.All(matchingItems, diff => Assert.Equal(diff.Item1, diff.Items));
            }

            if (expectedNotDefaultedItems == null)
            {
                Assert.All(diffs, diff => Assert.Empty(diff.NotDefaultedItems));
            }
            else
            {
                var expectedDiffItems = GetItems(expectedNotDefaultedItems);
                var matchingItems = diffs.Select(diff => (diff.NotDefaultedItems.Select(i => i.EvaluatedInclude), expectedDiffItems.SingleOrDefault(d => d.ItemType == diff.ItemType).Items));
                Assert.All(matchingItems, diff => Assert.Equal(diff.Item1, diff.Items));
            }

            if (expectedIntroducedItems == null)
            {
                Assert.All(diffs, diff => Assert.Empty(diff.IntroducedItems));
            }
            else
            {
                var expectedDiffItems = GetItems(expectedIntroducedItems);
                var matchingItems = diffs.Select(diff => (diff.IntroducedItems.Select(i => i.EvaluatedInclude), expectedDiffItems.SingleOrDefault(d => d.ItemType == diff.ItemType).Items));
                Assert.All(matchingItems, diff => Assert.Equal(diff.Item1, diff.Items));
            }

            if (expectedChangedItems == null)
            {
                Assert.All(diffs, diff => Assert.Empty(diff.ChangedItems));
            }
            else
            {
                var expectedDiffItems = GetItems(expectedChangedItems);
                var matchingItems = diffs.Select(diff => (diff.ChangedItems.Select(i => i.EvaluatedInclude), expectedDiffItems.SingleOrDefault(d => d.ItemType == diff.ItemType).Items));
                Assert.All(matchingItems, diff => Assert.Equal(diff.Item1, diff.Items));
            }
        }

        [Fact]
        public void ItemsDiff_GetLines()
        {
            var defaultedItems = IProjectFactory.Create(GetItems("A:B,C")).Items.ToImmutableArray();
            var removedItems = IProjectFactory.Create(GetItems("A:D,E")).Items.ToImmutableArray();
            var introducedItems = IProjectFactory.Create(GetItems("A:F,G")).Items.ToImmutableArray();
            var changedItems = ImmutableArray<IProjectItem>.Empty;
            var diff = new ItemsDiff("A", defaultedItems, removedItems, introducedItems, changedItems);

            var lines = diff.GetDiffLines();
            var expectedLines = new[]
            {
                "A items:",
                "- B",
                "- C",
                "= D",
                "= E",
                "+ F",
                "+ G",
                "",
            };

            Assert.Equal(expectedLines, lines);
        }

        [Fact]
        public void ItemsDiff_GetLines_Partial()
        {
            var defaultedItems = IProjectFactory.Create(GetItems("X:Y,Z")).Items.ToImmutableArray();
            var removedItems = ImmutableArray<IProjectItem>.Empty;
            var introducedItems = ImmutableArray<IProjectItem>.Empty;
            var changedItems = ImmutableArray<IProjectItem>.Empty;
            var diff = new ItemsDiff("X", defaultedItems, removedItems, introducedItems, changedItems);

            var lines = diff.GetDiffLines();
            var expectedLines = new[]
            {
                "X items:",
                "- Y",
                "- Z",
                "",
            };

            Assert.Equal(expectedLines, lines);

        }
        /// <summary>
        /// Expected format here is "A:B,C;C:D,E"
        /// </summary>
        private static IEnumerable<(string ItemType, string[] Items)> GetItems(string projectItems)
        {
            var lines = projectItems.Split(';');

            var items = from line in lines
                        let splitItems = line.Split(':')
                        select (ItemType: splitItems[0], Items: splitItems[1].Split(','));

            return items;
        }

    }
}
