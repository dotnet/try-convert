using System.Collections.Generic;

namespace MSBuildSdkDiffer
{
    public class ProjectItemComparer : IEqualityComparer<IProjectItem>
    {
        private ProjectItemComparer() { }

        public static ProjectItemComparer Instance = new ProjectItemComparer();

        public bool Equals(IProjectItem x, IProjectItem y)
        {
            return x.ItemType == y.ItemType && x.EvaluatedInclude == y.EvaluatedInclude;
        }

        public int GetHashCode(IProjectItem obj)
        {
            return (obj.EvaluatedInclude + obj.ItemType).GetHashCode();
        }
    }
}