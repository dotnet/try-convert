using System.Collections.Generic;
using System.Linq;

namespace MSBuildSdkDiffer
{
    public class ProjectItemComparer : IEqualityComparer<IProjectItem>
    {
        private readonly bool _compareMetadata;
        
        public static ProjectItemComparer IncludeComparer = new ProjectItemComparer(compareMetadata: false);
        public static ProjectItemComparer MetadataComparer = new ProjectItemComparer(compareMetadata: true);

        private ProjectItemComparer(bool compareMetadata)
        {
            _compareMetadata = compareMetadata;
        }

        public bool Equals(IProjectItem x, IProjectItem y)
        {
            var metadataEqual = _compareMetadata ? x.DirectMetadata.SequenceEqual(y.DirectMetadata) : true;

            return x.ItemType == y.ItemType && x.EvaluatedInclude == y.EvaluatedInclude && metadataEqual;
        }

        public int GetHashCode(IProjectItem obj)
        {
            return (obj.EvaluatedInclude + obj.ItemType).GetHashCode();
        }
    }
}