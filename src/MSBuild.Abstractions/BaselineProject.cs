using System;
using System.Collections.Immutable;
using System.Linq;
using MSBuild.Conversion.Facts;

namespace MSBuild.Abstractions
{
    public struct BaselineProject
    {
        public readonly ImmutableArray<string> GlobalProperties;
        public readonly ImmutableDictionary<string, string> TargetProjectProperties;
        public readonly UnconfiguredProject Project;
        public readonly ProjectStyle ProjectStyle;

        public BaselineProject(UnconfiguredProject project, ImmutableArray<string> globalProperties, ProjectStyle projectStyle) : this()
        {
            GlobalProperties = globalProperties;
            Project = project ?? throw new ArgumentNullException(nameof(project));
            ProjectStyle = projectStyle;
        }

        public string GetTfm()
        {
            if (GlobalProperties.Contains(MSBuildFacts.TargetFrameworkNodeName, StringComparer.OrdinalIgnoreCase))
            {
                // The original project had a TargetFramework property. No need to add it again.
                return GlobalProperties.First(p => p.Equals(MSBuildFacts.TargetFrameworkNodeName, StringComparison.OrdinalIgnoreCase));
            }
            var rawTFM = Project.FirstConfiguredProject.GetProperty(MSBuildFacts.TargetFrameworkNodeName)?.EvaluatedValue;
            if (rawTFM == null)
            {
                throw new InvalidOperationException(
                    $"{MSBuildFacts.TargetFrameworkNodeName} is not set in {nameof(Project.FirstConfiguredProject)}");
            }

            // This is pretty much never gonna happen, but it was cheap to write the code
            return MSBuildHelpers.IsNotNetFramework(rawTFM) ? StripDecimals(rawTFM) : rawTFM;

            static string StripDecimals(string tfm)
            {
                var parts = tfm.Split('.');
                return string.Join("", parts);
            }
        }
    }
}
