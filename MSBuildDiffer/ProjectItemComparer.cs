using System.Collections.Generic;
using Microsoft.Build.Evaluation;

namespace MSBuildDiffer
{
    public class ProjectItemComparer : IEqualityComparer<ProjectItem>
    {
        private ProjectItemComparer() { }

        public static ProjectItemComparer Instance = new ProjectItemComparer();

        public bool Equals(ProjectItem x, ProjectItem y)
        {
            return x.ItemType == y.ItemType && x.EvaluatedInclude == y.EvaluatedInclude;
        }

        public int GetHashCode(ProjectItem obj)
        {
            return (obj.EvaluatedInclude + obj.ItemType).GetHashCode();
        }
    }
}