using System;
using System.Collections.Immutable;

namespace MSBuildSdkDiffer
{
    internal struct BaselineProject
    {
        public readonly ImmutableArray<string> GlobalProperties;
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
