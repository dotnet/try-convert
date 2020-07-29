using System;
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

        public const string PackageReferenceName = "PackageReference";
        public const string NetCore5 = "net5.0";
    }
}
