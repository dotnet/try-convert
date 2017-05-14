using System.Collections.Immutable;
using System.Linq;
using MSBuildSdkDiffer.Tests.Mocks;
using Xunit;

namespace MSBuildSdkDiffer.Tests
{
    public class PropertiesDiffTests
    {
        [Theory]
        [InlineData("A=B", "A", "A=B", "A", null, null)]
        [InlineData("A=B", "A", "D=E", null, "A", null)]
        [InlineData("A=B;C=D", "A", "C=D", null, "A", null)]
        [InlineData("A=B;C=D", "A", "A=C", null, null, "A")]
        [InlineData("A=B;C=D", "A;C", "C=E", null, "A", "C")]
        [InlineData("A=B;C=D;E=F", "A;C;E", "C=E;E=F", "E", "A", "C")]
        public void PropertiesDiff(string projectProps, string propsInFile, string sdkBaselineProps, string expectedDefaultedProps, string expectedNotDefaultedProps, string expectedChangedProps)
        {
            var project = IProjectFactory.Create(projectProps, propsInFile);
            var sdkBaselineProject = IProjectFactory.Create(sdkBaselineProps, propsInFile);

            var differ = new Differ(project, sdkBaselineProject);

            var diff = differ.GetPropertiesDiff();

            if (expectedDefaultedProps == null)
            {
                Assert.Empty(diff.DefaultedProperties);
            }
            else
            {
                Assert.Equal(diff.DefaultedProperties.Select(p=> p.Name), expectedDefaultedProps.Split(';'));
            }

            if (expectedNotDefaultedProps == null)
            {
                Assert.Empty(diff.NotDefaultedProperties);
            }
            else
            {
                Assert.Equal(diff.NotDefaultedProperties.Select(p => p.Name), expectedNotDefaultedProps.Split(';'));
            }

            if (expectedChangedProps == null)
            {
                Assert.Empty(diff.ChangedProperties);
            }
            else
            {
                Assert.Equal(diff.ChangedProperties.Select(p => p.oldProp.Name), expectedChangedProps.Split(';'));
            }
        }

        [Fact]
        public void PropertiesDiff_GetLines()
        {
            var defaultedProps = IProjectFactory.Create("A=B;C=D").Properties.ToImmutableArray();
            var removedProps = IProjectFactory.Create("E=F;G=H").Properties.ToImmutableArray();
            var changedProps = IProjectFactory.Create("I=J").Properties.Zip(IProjectFactory.Create("I=K").Properties, (a, b) => (a, b)).ToImmutableArray();
            var diff = new PropertiesDiff(defaultedProps, removedProps, changedProps);

            var lines = diff.GetDiffLines();
            var expectedLines = new[]
            {
                "Properties that are defaulted by the SDK:",
                "- A = B",
                "- C = D",
                "",
                "Properties that are not defaulted by the SDK:",
                "+ E = F",
                "+ G = H",
                "",
                "Properties whose value is different from the SDK's default:",
                "- I = J",
                "+ I = K",
                ""
            };

            Assert.Equal(expectedLines, lines);
        }

    }
}
