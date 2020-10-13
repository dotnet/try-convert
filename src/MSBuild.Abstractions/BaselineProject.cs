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
        public readonly ProjectOutputType OutputType;
        public readonly string TargetTFM;

        public BaselineProject(UnconfiguredProject project, ImmutableArray<string> globalProperties, ProjectStyle projectStyle, ProjectOutputType outputType, string candidateTargetTFM, bool keepCurrentTFMs) : this()
        {
            GlobalProperties = globalProperties;
            Project = project ?? throw new ArgumentNullException(nameof(project));
            ProjectStyle = projectStyle;
            OutputType = outputType;
            TargetTFM = keepCurrentTFMs
                ? GetCurrentTFM(globalProperties, project)
                : AdjustTargetTFM(projectStyle, outputType, candidateTargetTFM);
        }

        private static string AdjustTargetTFM(ProjectStyle projectStyle, ProjectOutputType outputType, string candidateTargetTFM)
        {
            if (candidateTargetTFM.ContainsIgnoreCase(MSBuildFacts.Net5) && projectStyle is ProjectStyle.WindowsDesktop)
            {
                return MSBuildFacts.Net5Windows;
            }

            if (projectStyle is not ProjectStyle.MSTest && outputType is ProjectOutputType.Library)
            {
                return MSBuildFacts.NetStandard20;
            }

            return candidateTargetTFM;
        }

        private static string GetCurrentTFM(ImmutableArray<string> globalProperties, UnconfiguredProject project)
        {
            if (globalProperties.Contains(MSBuildFacts.TargetFrameworkNodeName, StringComparer.OrdinalIgnoreCase))
            {
                // The original project had a TargetFramework property. No need to add it again.
                return globalProperties.First(p => p.Equals(MSBuildFacts.TargetFrameworkNodeName, StringComparison.OrdinalIgnoreCase));
            }
            var rawTFM = project.FirstConfiguredProject.GetProperty(MSBuildFacts.TargetFrameworkNodeName)?.EvaluatedValue;
            if (rawTFM == null)
            {
                throw new InvalidOperationException(
                    $"{MSBuildFacts.TargetFrameworkNodeName} is not set in {nameof(project.FirstConfiguredProject)}");
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
