using Conversion;
using MSBuildAbstractions;
using System.IO;
using Xunit;

namespace SmokeTests
{
    public class BasicSmokeTests
    {
        [Fact]
        public void ConvertsLegacyFSharpConsole()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild();
            var loader = new ProjectLoader();
            var path = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.LegacyFSharpConsole", "SmokeTests.LegacyFSharpConsole.fsproj");
            loader.LoadProjects(path);
            var converter = new Converter(loader.Project, loader.SdkBaselineProject, loader.ProjectRootElement, loader.ProjectRootDirectory);
            var root = converter.GenerateProjectFile();

            // TODO - figure out how to compare against a baseline

            Assert.True(true);
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplate()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild();
            var loader = new ProjectLoader();
            var path = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.WpfFramework", "SmokeTests.WpfFramework.csproj");
            loader.LoadProjects(path);
            var converter = new Converter(loader.Project, loader.SdkBaselineProject, loader.ProjectRootElement, loader.ProjectRootDirectory);
            var str = converter.GenerateProjectFile();
            Assert.True(true);
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplate()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild();
            var loader = new ProjectLoader();
            var path = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.WinformsFramework", "SmokeTests.WinformsFramework.csproj");
            loader.LoadProjects(path);
            var converter = new Converter(loader.Project, loader.SdkBaselineProject, loader.ProjectRootElement, loader.ProjectRootDirectory);
            var str = converter.GenerateProjectFile();
            Assert.True(true);
        }
    }
}
