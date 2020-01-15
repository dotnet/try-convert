using System;
using System.Threading;

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
                MSBuildHelpers.HookAssemblyResolveForMSBuild();
            }
        }

        public void Dispose()
        {
        }
    }
}
