using System;
using System.Collections.Immutable;

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
    }
}
