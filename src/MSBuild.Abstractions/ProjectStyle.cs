namespace MSBuild.Abstractions
{
    public enum ProjectStyle
    {
        /// <summary>
        /// The project has an import of two defaults. Typically Common.props and CSharp.targets or FSharp.targets, etc. 
        /// </summary>
        Default,

        /// <summary>
        /// Using one of the two defaults, typically CSharp.targets or FSharp.targets.
        /// </summary>
        DefaultSubset,

        /// <summary>
        /// The project imports props and targets but not the default ones. 
        /// </summary>
        DefaultWithCustomTargets,

        /// <summary>
        /// Has more imports and the shape is unknown.
        /// </summary>
        Custom,

        /// <summary>
        /// The project is WPF or WinForms, and will use the WinDesktop framework reference
        /// </summary>
        WindowsDesktop
    }
}
