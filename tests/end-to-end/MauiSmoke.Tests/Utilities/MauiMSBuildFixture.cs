using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Locator;
using MSBuild.Abstractions;

namespace MauiSmoke.Tests.Utilities
{
    /// <summary>
    /// This test fixture ensures that MSBuild is loaded from VS Install Directory
    /// </summary>
    public class MauiMSBuildFixture : IDisposable
    {
        private static int _registered = 0;

        public void MSBuildPathForXamarinProject()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 0)
            {
                // During testing we just need a default MSBuild instance to be registered.
                var defaultInstance = MSBuildLocator.QueryVisualStudioInstances().First();
                MSBuildHelpers.HookAssemblyResolveForMSBuild(defaultInstance.MSBuildPath);

                // For Xamarin support we need to set the MSBuild Extensions path to VS install.
                var vsinstalldir = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSINSTALLDIR"))
                    ? Environment.GetEnvironmentVariable("VSINSTALLDIR")
                    : new VisualStudioLocator().GetLatestVisualStudioPath();
                if (!string.IsNullOrEmpty(vsinstalldir))
                {
                    Environment.SetEnvironmentVariable("MSBuildExtensionsPath", Path.Combine(vsinstalldir, "MSBuild"));
                }
                else
                {
                    Console.WriteLine("Error locating VS Install Directory. Try setting Environment Variable VSINSTALLDIR.");
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
