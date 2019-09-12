using System;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace PackageConversion.Tests
{
    public class ParsingTests
    {
        [Fact]
        public void FailsWithNullPath()
        {
            Assert.Throws<ArgumentException>(() => PackagesConfigConverter.Convert(null));
        }

        [Fact]
        public void FailsWithNotBeingAPackagesConfigFile()
        {
            Assert.Throws<ArgumentException>(() => PackagesConfigConverter.Convert("/path/to/file.text"));
        }

        [Fact]
        public void ParseNoPackagesNodeFails()
        {
            var packagesConfig = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<doot>toot</doot>
";

            var doc = XDocument.Parse(packagesConfig.Trim());
            Assert.Throws<PackagesConfigHasNoPackagesException>(() => PackagesConfigParser.ParseDocument(doc));
        }

        [Fact]
        public void ParseNoValidIdFails()
        {
            var packagesConfig = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""FSharp.Core"" version=""4.6.2"" targetFramework=""net472"" />
  <package id="""" version=""4.4.0"" targetFramework=""net472"" />
</packages>
";

            var doc = XDocument.Parse(packagesConfig.Trim());
            Assert.Throws<PackagesConfigHasInvalidPackageNodesException>(() => PackagesConfigParser.ParseDocument(doc));
        }

        [Fact]
        public void ParseNoValidVersionFails()
        {
            var packagesConfig = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""FSharp.Core"" version="""" targetFramework=""net472"" />
  <package id=""System.ValueTuple"" version=""4.4.0"" targetFramework=""net472"" />
</packages>
";

            var doc = XDocument.Parse(packagesConfig.Trim());
            Assert.Throws<PackagesConfigHasInvalidPackageNodesException>(() => PackagesConfigParser.ParseDocument(doc));
        }

        [Fact]
        public void ParseDefaultLegacyPacakgesConfigInFSharpTemplatesSucceeds()
        {
            var packagesConfig = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<packages>
  <package id=""FSharp.Core"" version=""4.6.2"" targetFramework=""net472"" />
  <package id=""System.ValueTuple"" version=""4.4.0"" targetFramework=""net472"" />
</packages>
";

            var doc = XDocument.Parse(packagesConfig.Trim());
            var packages = PackagesConfigParser.ParseDocument(doc);

            Assert.True(packages.Count() == 2);

            var fsharpCore = packages.First();
            var svt = packages.Skip(1).Single();

            Assert.Equal("FSharp.Core", fsharpCore.ID);
            Assert.Equal("4.6.2", fsharpCore.Version);
            Assert.Equal("net472", fsharpCore.TargetFramework);
            Assert.False(fsharpCore.IsPreview);
            Assert.False(fsharpCore.DevelopmentDependency);
            Assert.True(string.IsNullOrWhiteSpace(fsharpCore.AllowedVersions));

            Assert.Equal("System.ValueTuple", svt.ID);
            Assert.Equal("4.4.0", svt.Version);
            Assert.Equal("net472", svt.TargetFramework);
            Assert.False(svt.IsPreview);
            Assert.False(svt.DevelopmentDependency);
            Assert.True(string.IsNullOrWhiteSpace(svt.AllowedVersions));
        }
    }
}
