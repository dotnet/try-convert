using System;
using System.Collections.Generic;
using System.Text;

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

    public class PackagesConfigHasMultiplePackagesElements : Exception
    {
        public PackagesConfigHasMultiplePackagesElements()
        {
        }

        public PackagesConfigHasMultiplePackagesElements(string message) : base(message)
        {
        }
    }
}
