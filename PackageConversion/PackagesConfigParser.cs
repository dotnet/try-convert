using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

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
                throw new ArgumentException($"'{nameof(path)}' is not a 'packages.config' file, which it should be.");
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

            bool ValidPackageNode(XElement pkgNode) =>
                pkgNode.Element("id") is object
                && !string.IsNullOrWhiteSpace(pkgNode.Element("id").Value)
                && pkgNode.Element("version") is object
                && string.IsNullOrWhiteSpace(pkgNode.Element("version").Value);

            var packagesNode =
                from nd in doc.Nodes()
                where nd.NodeType == XmlNodeType.Element
                select nd as XElement;

            if (packagesNode is null || !packagesNode.Any(node => node.Name == "packages"))
            {
                throw new PackagesConfigHasNoPackagesException("Parsed XML document has no '<packages>' element.");
            }

            var packages =
                from nd in packagesNode.Single().Nodes()
                where nd.NodeType == XmlNodeType.Element
                select nd as XElement;

            if (!packages.All(ValidPackageNode))
            {
                throw new PackagesConfigHasInvalidPackageNodesException("Not all packages have a valid 'id' or 'version' field.");
            }

            return packages.Select(ParsePackageConfig);
        }
    }
}
