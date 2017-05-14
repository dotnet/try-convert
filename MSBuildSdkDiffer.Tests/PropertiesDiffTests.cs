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
        public void PropertiesDiff_GetLines(string projectProps, string propsInFile, string sdkBaselineProps, string expectedDefaultedProps, string expectedNotDefaultedProps, string expectedChangedProps)
        {
            var project = IProjectFactory.Create(projectProps);
            var sdkBaselineProject = IProjectFactory.Create(sdkBaselineProps);

            var differ = new Differ(project, propsInFile.Split(';'), sdkBaselineProject);

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
    }
}
