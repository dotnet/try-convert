using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MSBuildAbstractions
{
    public class MSBuildWorkspaceItem
    {
        public IProjectRootElement ProjectRootElement { get; }
        public UnconfiguredProject UnconfiguredProject { get; }
        public BaselineProject SdkBaselineProject { get; }
        public DirectoryInfo RootDirectory { get; }

        public MSBuildWorkspaceItem(IProjectRootElement root, UnconfiguredProject unconfiguredProject, BaselineProject baseline, DirectoryInfo rootDirectory)
        {
            ProjectRootElement = root;
            UnconfiguredProject = unconfiguredProject;
            SdkBaselineProject = baseline;
            RootDirectory = rootDirectory;
        }
    }
}
