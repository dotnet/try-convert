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
        /// Set of NuGet packages incompatible with WinUI3
        /// </summary>
        public static ImmutableArray<string> IncompatiblePackages => ImmutableArray.Create(
            "Put.Bad.Pkg.Here"
        );

       

        public static ImmutableDictionary<string, string> ConvertiblePackages => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Microsoft.UI.Xaml", "Microsoft.WinUI" }
        });

        public static ImmutableDictionary<string, string> PackageVersions => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Microsoft.WinUI", "NeedToAddVersions" }
        });

        public const string PackageReferenceName = "PackageReference";
        public const string NetCore5 = "net5.0";
        public const string VSVersionGroup = "VisualStudioVersion";
        public const string MSBIncompatImport = "Xaml.CSharp.targets";
        public const string MSBIncompatReplace = @"$(MSBuildToolsPath)\Microsoft.CSharp.targets";
    }
}
