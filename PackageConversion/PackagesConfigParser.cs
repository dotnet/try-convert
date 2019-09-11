using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

[assembly: InternalsVisibleTo("PackageConversion.Tests")]

namespace PackageConversion
{
    internal static class PackagesConfigParser
    {
        internal static IEnumerable<PackagesConfigPackage> Parse(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException($"'{nameof(path)}' is null or empty.");
            }

            if (!path.EndsWith("packages.config", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"'{nameof(path)}' is not 'packages.config', which it should be.");
            }

            var text = File.ReadAllText(path);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException($"Text read from '{nameof(path)}' is empty.");
            }

            var doc = XDocument.Parse(text);
            if (doc is null)
            {
                throw new ArgumentException($"Contents of '{nameof(path)}' cannot parse as XML.");
            }

            return ParseDocument(doc);
        }

        internal static IEnumerable<PackagesConfigPackage> ParseDocument(XDocument doc)
        {
            PackagesConfigPackage ParsePackageConfig(XElement element) =>
                new PackagesConfigPackage
                {
                    // Required
                    ID = element.Attribute("id").Value,

                    // Required
                    Version = element.Attribute("version").Value,

                    // The rest are optional
                    TargetFramework = element.Attribute("targetFramework") is null ? string.Empty : element.Attribute("targetFramework").Value,
                    AllowedVersions = element.Attribute("allowedVersions") is null ? string.Empty : element.Attribute("allowedVersions").Value,
                    DevelopmentDependency = element.Attribute("allowedVersions") is null ? false : bool.Parse(element.Attribute("developmentDependency").Value),
                    IsPreview = element.Attribute("version").Value.EndsWith("-preview")
                };

            var packagesNode = from nd in doc.Nodes() where nd.NodeType == XmlNodeType.Element select nd as XElement;
            if (packagesNode is null || !packagesNode.Any())
            {
                throw new PackagesConfigHasNoPackagesException("Parsed XML document has no '<packages>' element.");
            }

            if (packagesNode.Count() > 1)
            {
                // lol if this is actually allowed though
                throw new PackagesConfigHasMultiplePackagesElements("Parsed XML document has multuple '<packages>' elements, which isn't allowed.");
            }

            var packagesNodes = from nd in packagesNode.Single().Nodes() where nd.NodeType == XmlNodeType.Element select nd as XElement;

            return from n in packagesNodes select ParsePackageConfig(n);
        }
    }
}
