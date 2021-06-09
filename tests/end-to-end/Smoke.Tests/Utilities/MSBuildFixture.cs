using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Locator;
using MSBuild.Abstractions;

namespace Smoke.Tests.Utilities
{
    /// <summary>
    /// This test fixture ensures that MSBuild is loaded.
    /// </summary>
    public class MSBuildFixture : IDisposable
    {
        private static int _registered = 0;

        public void RegisterInstance()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 0)
            {
                // During testing we just need a default MSBuild instance to be registered.
                var defaultInstance = MSBuildLocator.QueryVisualStudioInstances().First();
                MSBuildHelpers.HookAssemblyResolveForMSBuild(defaultInstance.MSBuildPath);
            }
        }

        public void MSBuildPathForXamarinProject()
        {
            if (Interlocked.Exchange(ref _registered, 1) == 0)
            {
                //For Xamarin Project tests, default MSBuild instance resolved from VSINSTALLDIR Environment Variable
                var vsinstalldir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
                if (!string.IsNullOrEmpty(vsinstalldir))
                {
                    MSBuildHelpers.HookAssemblyResolveForMSBuild(Path.Combine(vsinstalldir, "MSBuild", "Current", "Bin"));
                }
                else
                {
                    string vsPath = new VisualStudioLocator().GetLatestVisualStudioPath();
                    if (string.IsNullOrWhiteSpace(vsPath))
                        throw new Exception("Error locating VS Install Directory. Try setting Environment Variable VSINSTALLDIR.");
                    else
                        MSBuildHelpers.HookAssemblyResolveForMSBuild(Path.Combine(vsPath, "MSBuild", "Current", "Bin"));
                }
            }

        }

        public void Dispose()
        {
        }
    }
}

