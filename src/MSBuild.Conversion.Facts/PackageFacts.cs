namespace MSBuild.Conversion.Facts
{
    /// <summary>
    /// A bunch of known values regarding NuGet.
    /// </summary>
    public static class PackageFacts
    {
        public const string PackagesConfigIncludeName = "packages.config";
        public const string PackageReferenceItemType = "PackageReference";
        public const string PackageReferenceIDName = "id";
        public const string PackageReferenceVersionName = "version";
        public const string PackageReferencePackagesNodeName = "packages";
        public const string EnsureNuGetPackageBuildImportsName = "EnsureNuGetPackageBuildImports";
    }
}
