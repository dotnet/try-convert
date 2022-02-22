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
        public void Dispose()
        {
        }
    }
}

