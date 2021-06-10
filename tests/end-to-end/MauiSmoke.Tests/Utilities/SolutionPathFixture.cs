using System;
using System.IO;
using System.Threading;

namespace MauiSmoke.Tests.Utilities
{
    /// <summary>
    /// This test fixture sets the <see cref="Environment.CurrentDirectory" /> to the try-convert solution's path.
    /// </summary>
    public class MauiSolutionPathFixture : IDisposable
    {
        private static int _registered = 0;
        private static string _currentDirectory;

        public void SetCurrentDirectory()
        {
            if (Interlocked.Increment(ref _registered) == 1)
            {
                _currentDirectory = Environment.CurrentDirectory;
                var solutionPath = Directory.GetParent(_currentDirectory).Parent.Parent.Parent.Parent.FullName;
                Environment.CurrentDirectory = solutionPath;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Decrement(ref _registered) == 0)
            {
                Environment.CurrentDirectory = _currentDirectory;
                _currentDirectory = null;
            }
        }
    }
}
