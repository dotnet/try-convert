using System.Collections.Immutable;

namespace Facts
{
    /// <summary>
    ///  A bunch of known values regarding desktop projects.
    /// </summary>
    public static class DesktopFacts
    {
        /// <summary>
        /// For use with conversion of WinForms and WPF projects only.
        /// </summary>
        public static ImmutableArray<string> ReferencesThatNeedRemoval => ImmutableArray.Create(
            "System.Deployment"
        );

        /// <summary>
        /// The core set of references all desktop WPF projects use.
        /// </summary>
        /// <remarks>
        /// Desktop projects will only convert to .NET Core, so any item includes that have .NET Core equivalents will be removed.
        /// Users will have to ensure those packages are also added if they cannot do so with a tool.
        /// References that are already present will also be removed.
        /// </remarks>
        public static ImmutableArray<string> KnownWPFReferences => ImmutableArray.Create(
            "System.Xaml",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework"
        );

        /// <summary>
        /// The core set of references all desktop WinForms projects use.
        /// </summary>
        /// <remarks>
        /// Desktop projects will only convert to .NET Core, so any item includes that have .NET Core equivalents will be removed.
        /// Users will have to ensure those packages are also added if they cannot do so with a tool.
        /// References that are already present will also be removed.
        /// </remarks>
        public static ImmutableArray<string> KnownWinFormsReferences => ImmutableArray.Create(
            "System.Windows.Forms",
            "System.Deployment",
            "System.Drawing"
        );

        public const string WinSDKAttribute = "Microsoft.NET.Sdk.WindowsDesktop";
        public const string DefaultSDKAttribute = "Microsoft.NET.Sdk";
        public const string UseWPFPropertyName = "UseWPF";
        public const string UseWinFormsPropertyName = "UseWindowsForms";
        public const string DesignerSuffix = ".Designer.cs";
        public const string XamlFileExtension = ".xaml";
        public const string SettingsDesignerFileName = "Settings.Designer.cs";
        public const string SettingsFileSuffix = ".settings";
        public const string ResourcesDesignerFileName = "Resources.Designer.cs";
        public const string ResourcesFileSuffix = ".resx";
        public const string FormSubTypeValue = "Form";
    }
}
