using System.Collections.Immutable;

namespace MSBuild.Conversion.Facts
{
    public static class WebFacts
    {
        public const string WebSDKAttribute = "Microsoft.NET.Sdk.Web";
        public const string MvcBuildViewsName = "MvcBuildViews";
        public const string WebProjectPropertiesName = "WebProjectProperties";
        public const string WebApplicationTargets = "Microsoft.WebApplication.targets";

        /// <summary>
        /// The core set of references all ASP.NET projects use.
        /// </summary>
        public static ImmutableArray<string> KnownWebReferences => ImmutableArray.Create(
            "System.Web"
        );
    }
}
