using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.Build.Construction;

using MSBuild.Abstractions;
using MSBuild.Conversion.Project;

using Smoke.Tests.Utilities;

using Xunit;

namespace SmokeTests
{
    public class BasicSmokeTests : IClassFixture<SolutionPathFixture>, IClassFixture<MSBuildFixture>
    {
        private string SolutionPath => Environment.CurrentDirectory;
        private string TestDataPath => Path.Combine(SolutionPath, "tests", "TestData");
        private string GetFSharpProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.fsproj");
        private string GetCSharpProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.csproj");
        private string GetVisualBasicProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.vbproj");
        private SharedTestLogic _testLogic => new SharedTestLogic();

        public BasicSmokeTests(SolutionPathFixture solutionPathFixture, MSBuildFixture msBuildFixture)
        {
            msBuildFixture.RegisterInstance();
            solutionPathFixture.SetCurrentDirectory();
        }

        [Fact]
        public void ConvertsLegacyFSharpConsoleToNetCoreApp31()
        {
            var projectToConvertPath = GetFSharpProjectPath("SmokeTests.LegacyFSharpConsole");
            var projectBaselinePath = GetFSharpProjectPath("SmokeTests.FSharpConsoleCoreBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsLegacyFSharpConsoleToNet50()
        {
            var projectToConvertPath = GetFSharpProjectPath("SmokeTests.LegacyFSharpConsole");
            var projectBaselinePath = GetFSharpProjectPath("SmokeTests.FSharpConsoleNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0");
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplateForNetCoreApp31()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WpfFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WpfCoreBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WpfFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WpfNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsWpfVbFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetVisualBasicProjectPath("SmokeTests.WpfVbFramework");
            var projectBaselinePath = GetVisualBasicProjectPath("SmokeTests.WpfVbNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsWinformsVbFrameworkTemplateAndKeepTargetFrameworkMoniker()
        {
            var projectToConvertPath = GetVisualBasicProjectPath("SmokeTests.WinformsVbFramework");
            var projectBaselinePath = GetVisualBasicProjectPath("SmokeTests.WinformsVbKeepTfm");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "testdata", keepTargetFramework: true);
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplateForNetCoreApp31()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WinformsFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WinformsCoreBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WinformsFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WinformsNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsLegacyMSTest()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyMSTest");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.MSTestCoreBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsLegacyMSTestVB()
        {
            var projectToConvertPath = GetVisualBasicProjectPath("SmokeTests.LegacyMSTestVB");
            var projectBaselinePath = GetVisualBasicProjectPath("SmokeTests.MSTestVbNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsLegacyWebLibraryToNetFx()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyWebLibrary");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WebLibraryNetFxBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net472", true);
        }

        [Fact]
        public void ConvertsLegacyWebLibraryToNet5()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyWebLibrary");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WebLibraryNet5Baseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0", true);
        }
    }
}
