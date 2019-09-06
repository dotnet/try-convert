using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProjectSimplifier
{
    internal static class Facts
    {
        /// <summary>
        /// Props files which are known to be imported in standard projects created from templates that can be converted to use the SDK
        /// </summary>
        public static ImmutableArray<string> PropsConvertibleToSDK => ImmutableArray.Create("Microsoft.Common.props");

        /// <summary>
        /// Targets files which are known to be imported in standard projects created from templates that can be converted to use the SDK.
        /// </summary>
        public static ImmutableArray<string> TargetsConvertibleToSDK => ImmutableArray.Create(
            "Microsoft.CSharp.targets",
            "Microsoft.VisualBasic.targets",
            "Microsoft.Portable.CSharp.targets",
            "Microsoft.Portable.VisualBasic.targets",
            "Microsoft.FSharp.Targets");

        /// <summary>
        /// Mapping of PCL profiles to netstandard versions.
        /// </summary>
        public static ImmutableDictionary<string, string> PCLToNetStandardVersionMapping => ImmutableDictionary.CreateRange(new Dictionary<string, string>
        {
            { "Profile7",        "1.1"  },
            { "Profile31",       "1.0"  },
            { "Profile32",       "1.2"  },
            { "Profile44",       "1.2"  },
            { "Profile49",       "1.0"  },
            { "Profile78",       "1.0"  },
            { "Profile84",       "1.0"  },
            { "Profile111",      "1.0"  },
            { "Profile151",      "1.0"  },
            { "Profile157",      "1.0"  },
            { "Profile259",      "1.0"  },
        });

        public static ImmutableArray<string> PropertiesNotNeededInCPS => ImmutableArray.Create(
            "ProjectGuid", // Guids are in-memory in CPS
            "ProjectTypeGuids", // Not used - capabilities are used instead
            "TargetFrameworkIdentifier", // Inferred from TargetFramework
            "TargetFrameworkVersion", // Inferred from TargetFramework
            "TargetFrameworkProfile" // Inferred from TargetFramework
            );

        public static ImmutableArray<string> GlobbedItemTypes => ImmutableArray.Create(
            "Compile",
            "EmbeddedResource",
            "None"
            );

        public const string SharedProjectsImportLabel = "Shared";
    }
}
