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
            "Microsoft.FSharp.Targets"
        );

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

        public static ImmutableArray<string> GlobbedItemTypes => ImmutableArray.Create(
            "Compile",
            "EmbeddedResource",
            "None",
            "Page",
            "ApplicationDefinition"
            );

        public static ImmutableArray<string> UnnecessaryProperties => ImmutableArray.Create(
            // The following are unecessary in CPS and/or are already in the .NET SDK
            "ProjectGuid",
            "ProjectTypeGuids",
            "TargetFrameworkIdentifier",
            "TargetFrameworkVersion",
            "TargetFrameworkProfile",
            "FSharpTargetsPath",

            // The following are rarely (if ever) set by users, but are defaulted in some templates,
            // and are likely to have incorrect values for .NET SDK/VS interaction
            "MinimumVisualStudioVersion",
            "SchemaVersion",
            "Name",
            "Prefer32Bit",
            "DocumentationFile",
            "VSToolsPath",
            "VisualStudioVersion"
        );

        public static ImmutableArray<string> DefaultDefineConstants => ImmutableArray.Create(
            "DEBUG",
            "TRACE"
        );

        public static ImmutableArray<string> DefaultOutputPaths => ImmutableArray.Create(
            @"bin\Release\",
            @"bin\Debug\"
        );

        public static ImmutableArray<string> DefaultPlatformTargets => ImmutableArray.Create(
            "AnyCPU"
        );

        public static ImmutableArray<string> DefaultDebugTypes => ImmutableArray.Create(
            "full",
            "pdbonly"
        );

        public static ImmutableArray<string> UnnecessaryItemIncludes => ImmutableArray.Create(
            // FSharp.Core is referenced by default in the .NET SDK
            "FSharp.Core",

            // F# explicitly references this in old-style projects; it's not needed now
            "mscorlib",

            // Microsoft.CSharp is already shipped as a part of the .NET SDK
            "Microsoft.CSharp",

            // App.config is now deprecated, user needs to use to appsettings.json
            "App.config",

            // packages.config is now deprecated, user needs to move to PackageReference
            "packages.config",

            // System.Net.Http is a part of the .NET SDK now
            "System.Net.Http"
        );

        public static ImmutableArray<string> ItemsWithPackagesThatWorkOnNETCore => ImmutableArray.Create(
            "System.Data.DataSetExtensions"
        );

        /// <summary>
        /// For use with conversion of WinForms and WPF projects only.
        /// </summary>
        public static ImmutableArray<string> DesktopReferencesThatNeedRemoval => ImmutableArray.Create(
            "System.Deployment"
        );

        public static ImmutableArray<string> KnownWPFReferences => ImmutableArray.Create(
            "System.Xaml",
            "WindowsBase",
            "PresentationCore",
            "PresentationFramework"
        );

        public static ImmutableArray<string> KnownWinFormsReferences => ImmutableArray.Create(
            "System.Windows.Forms",
            "System.Deployment",
            "System.Drawing"
        );

        public const string LowestFrameworkVersionWithSystemValueTuple = "net47";
        public const string SharedProjectsImportLabel = "Shared";
        public const string FSharpTargetsPathVariableName = @"$(FSharpTargetsPath)";
        public const string FSharpTargetsPath = @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\FSharp\Microsoft.FSharp.Targets";
        public const string WinSDKAttribute = "Microsoft.NET.Sdk.WindowsDesktop";
        public const string DefaultSDKAttribute = "Microsoft.NET.Sdk";
        public const string UseWPFPropertyName = "UseWPF";
        public const string UseWinFormsPropertyName = "UseWindowsForms";
        public const string NETCoreDesktopTFM = "netcoreapp3.0";
        public const string DesignerEndString = ".Designer.cs";
        public const string XamlFileExtension = ".xaml";
        public const string SettingsDesignerFileName = "Settings.Designer.cs";
        public const string ResourcesDesignerFileName = "Resources.Designer.cs";
        public const string SystemValueTupleName = "System.ValueTuple";
        public const string DefineConstantsName = "DefineConstants";
        public const string OutputPathName = "OutputPath";
        public const string DebugTypeName = "DebugType";
        public const string SubTypeName = "SubType";
        public const string DependentUponName = "DependentUpon";
        public const string PlatformTargetName = "PlatformTarget";
        public const string NetcoreappPrelude = "netcoreapp";
        public const string NetstandardPrelude = "netstandard";
        public const string MSBuildReferenceName = "Reference";
        public const string DesignerSubType = "Designer";
        public const string CodeSubType = "Code";
        public const string PackagesConfigIncludeName = "packages.config";
        public const string PackageReferenceItemType = "PackageReference";
    }
}
