using System.Collections.Generic;
using System.Linq;

namespace PackageConversion
{
    public static class PackagesConfigConverter
    {
        /// <summary>
        /// Given a path to a 'packages.config' file, gets an enumerable of package reference items.
        /// </summary>
        /// <param name="path">The path on disk to a specific packages.config file.</param>
        public static IEnumerable<PackageReferencePackage> Convert(string path) =>
            from pkg in PackagesConfigParser.Parse(path) select new PackageReferencePackage(pkg);
    }
}
