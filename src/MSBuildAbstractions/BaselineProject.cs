using System;
using System.Collections.Immutable;

namespace MSBuildAbstractions
{
    public struct BaselineProject
    {
        public readonly ImmutableArray<string> GlobalProperties;
        public readonly ImmutableDictionary<string, string> TargetProjectProperties;
        public readonly UnconfiguredProject Project;
        public readonly ProjectStyle ProjectStyle;

        public BaselineProject(UnconfiguredProject project, ImmutableArray<string> globalProperties, ImmutableDictionary<string, string> targetProjectProperties, ProjectStyle projectStyle) : this()
        {
            GlobalProperties = globalProperties;
            TargetProjectProperties = targetProjectProperties;
            Project = project ?? throw new ArgumentNullException(nameof(project));
            ProjectStyle = projectStyle;
        }
    }
}
