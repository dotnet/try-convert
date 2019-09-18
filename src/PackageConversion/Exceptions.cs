using System;

namespace PackageConversion
{
    public class PackagesConfigHasNoPackagesException : Exception
    {
        public PackagesConfigHasNoPackagesException()
        {
        }

        public PackagesConfigHasNoPackagesException(string message) : base(message)
        {
        }
    }

    public class PackagesConfigHasInvalidPackageNodesException : Exception
    {
        public PackagesConfigHasInvalidPackageNodesException()
        {
        }

        public PackagesConfigHasInvalidPackageNodesException(string message) : base(message)
        {
        }
    }
}
