using System.Collections.Generic;
using System.Collections.Immutable;

namespace MSBuild.Conversion.Facts
{
    /// <summary>
    ///  A bunch of known values regarding WinUI2 projects.
    /// </summary>
    public static class WinUIFacts
    {

        /// <summary>
        /// The core set of references WinUI projects use.
        /// </summary>
        /// <remarks>
        /// NEED to update some of these
        /// </remarks>
        public static ImmutableArray<string> KnownWinUIReferences => ImmutableArray.Create(
            "Microsoft.UI.Xaml",
            "Windows.UI.Xaml",
            "Microsoft.WinUI",
            "Microsoft.NETCore.UniversalWindowsPlatform"
        );

        /// <summary>
        /// Set of unecessary properties that are ok to remove
        /// </summary>
        public static ImmutableArray<string> UnnecessaryProperties => ImmutableArray.Create(
            "DefaultLanguage",
            "ProjectGuid", // Are these important? should they be converted instead?
            "ProjectTypeGuids", //same as above
            "SubType"

        );

        /// <summary>
        /// Set of NuGet packages incompatible with WinUI3 SDK NET5 Conversion
        /// </summary>
        public static ImmutableArray<string> IncompatiblePackages => ImmutableArray.Create(
            "Win2D.UWP"
        );

        /// <summary>
        /// Set of NuGet packages incompatible with WinUI3 UWP Conversion
        /// </summary>
        public static ImmutableArray<string> UWPIncompatiblePackages => ImmutableArray.Create(
            "Microsoft.Xaml.Behaviors.UWP"
        );

        public static ImmutableArray<string> WinUIRefs => ImmutableArray.Create(
            "Microsoft.UI.Xaml", 
            "Microsoft.WinUI" 
        );

        /// <summary>
        /// Set of items that belong in .wapproj file
        /// </summary>
        public static ImmutableArray<string> WapprojItems => ImmutableArray.Create(
            "AppxManifest"
        );
        /// <summary>
        /// Set of Properties that belong in .wapproj file
        /// </summary>
        public static ImmutableArray<string> WapprojProperties => ImmutableArray.Create(
            "PackageCertificateKeyFile",
            "GenerateAppInstallerFile",
            "AppxAutoIncrementPackageRevision",
            "AppxBundle",
            "AppxBundlePlatforms",
            "AppInstallerUpdateFrequency",
            "AppInstallerCheckForUpdateFrequency",
            "AppxPackageName",
            "AppxPackageSigningEnabled",
            "PackageCertificateThumbprint",
            "AppxPackageSigningTimestampDigestAlgorithm",
            "HoursBetweenUpdateChecks"
        );

        public static ImmutableDictionary<string, string> SDKDefaultProperties => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "TargetPlatformIdentifier", "UAP" },
            { "EnableTypeInfoReflection", "false" },
            { "IsWinUIAlpha", "true" },
            { "UseVSHostingProcess", "false"}
        });

        public static ImmutableDictionary<string, string> UWPConvertiblePackages => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Microsoft.UI.Xaml", "Microsoft.WinUI" }
        });

        public static ImmutableDictionary<string, string> ConvertiblePackages => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Microsoft.UI.Xaml", "Microsoft.WinUI" },
            { "Microsoft.Xaml.Behaviors.UWP", "Microsoft.Xaml.Behaviors.WinUI" },
            //{ "Win2D.UWP", "Microsoft.Win2D.WinUI" }, currently incompatible
            { "ColorCode.UWP", "ColorCode.WinUI" },
            { "Microsoft.Toolkit", "Microsoft.Toolkit" },
            { "Microsoft.Toolkit.HighPerformance", "Microsoft.Toolkit.HighPerformance" },
            { "Microsoft.Toolkit.Parsers", "Microsoft.Toolkit.Parsers" },
            { "Microsoft.Toolkit.Services", "Microsoft.Toolkit.Services" },
            { "Microsoft.Toolkit.Uwp", "Microsoft.Toolkit.Uwp" },
            { "Microsoft.Toolkit.Uwp.Connectivity", "Microsoft.Toolkit.Uwp.Connectivity" },
            { "Microsoft.Toolkit.Uwp.DeveloperTools", "Microsoft.Toolkit.Uwp.DeveloperTools" },
            { "Microsoft.Toolkit.Uwp.Input.GazeInteraction", "Microsoft.Toolkit.Uwp.Input.GazeInteraction" },
            { "Microsoft.Toolkit.Uwp.Notifications", "Microsoft.Toolkit.Uwp.Notifications" },
            { "Microsoft.Toolkit.Uwp.Notifications.JavaScript", "Microsoft.Toolkit.Uwp.Notifications.JavaScript" },
            { "Microsoft.Toolkit.Uwp.PlatformSpecificAnalyzer", "Microsoft.Toolkit.Uwp.PlatformSpecificAnalyzer" },
            { "Microsoft.Toolkit.Uwp.UI", "Microsoft.Toolkit.Uwp.UI" },
            { "Microsoft.Toolkit.Uwp.UI.Animations", "Microsoft.Toolkit.Uwp.UI.Animations" },
            { "Microsoft.Toolkit.Uwp.UI.Controls", "Microsoft.Toolkit.Uwp.UI.Controls" },
            { "Microsoft.Toolkit.Uwp.UI.Controls.DataGrid", "Microsoft.Toolkit.Uwp.UI.Controls.DataGrid" },
            { "Microsoft.Toolkit.Uwp.UI.Controls.Layout", "Microsoft.Toolkit.Uwp.UI.Controls.Layout" },
            { "Microsoft.Toolkit.Uwp.UI.Media", "Microsoft.Toolkit.Uwp.UI.Media" }
        });

        public static ImmutableDictionary<string, string> PackageVersions => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Microsoft.WinUI", "3.0.0-preview2.200713.0" },
            { "Microsoft.Xaml.Behaviors.WinUI", "NeedVersion" },// needs version
            //{ "Microsoft.Win2D.WinUI", "NeedsVersion"},// needs version Currently no version
            { "ColorCode.WinUI", "8.0.0-preview2" },
            { "Microsoft.Toolkit", "8.0.0-preview2" },
            { "Microsoft.Toolkit.HighPerformance", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Parsers", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Services", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.Connectivity", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.DeveloperTools", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.Input.GazeInteraction", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.Notifications", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.Notifications.JavaScript", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.PlatformSpecificAnalyzer", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI.Animations", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI.Controls", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI.Controls.DataGrid", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI.Controls.Layout", "8.0.0-preview2" },
            { "Microsoft.Toolkit.Uwp.UI.Media", "8.0.0-preview2" }
        });

        public const string MSBuildIncompatibleImport = "Microsoft.Windows.UI.Xaml.CSharp.targets";
        public const string UWPNuGetReference = "Microsoft.NETCore.UniversalWindowsPlatform";
        public const string MSBuildIncompatibleReplace = @"$(MSBuildToolsPath)\Microsoft.CSharp.targets";
        public const string UWPTargetPlatformValue = "UAP";
        public const string RdXmlFileExtension = ".rd.xml";
        public const string DotNetNativeReference = "DotNetNative";
    }
}
