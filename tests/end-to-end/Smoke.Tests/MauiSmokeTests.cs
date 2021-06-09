using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Construction;
using MSBuild.Abstractions;
using MSBuild.Conversion.Project;
using Smoke.Tests.Utilities;
using Xunit;

namespace Smoke.Tests
{
    public class MauiSmokeTests : IClassFixture<SolutionPathFixture>, IClassFixture<MSBuildFixture>
    {
        private string SolutionPath => Environment.CurrentDirectory;
        private string TestDataPath => Path.Combine(SolutionPath, "tests", "TestData");
        private string GetXamarinAndroidProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.csproj");
        private string GetXamariniOSProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.csproj");
        private SharedTestLogic _testLogic => new SharedTestLogic();

        public MauiSmokeTests(SolutionPathFixture solutionPathFixture, MSBuildFixture msBuildFixture)
        {
            msBuildFixture.MSBuildPathForXamarinProject();
            solutionPathFixture.SetCurrentDirectory();
        }

        [Fact]
        public void ConvertsXamarinFormsAndroidToMaui()
        {
            var projectToConvertPath = GetXamarinAndroidProjectPath("SmokeTests.XamarinForms.Android");
            var projectBaselinePath = GetXamarinAndroidProjectPath("SmokeTests.XamarinForms.AndroidBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net6.0-android");
        }

        [Fact]
        public void ConvertsXamarinFormsiOSToMaui()
        {
            var projectToConvertPath = GetXamariniOSProjectPath("SmokeTests.XamarinForms.iOS");
            var projectBaselinePath = GetXamariniOSProjectPath("SmokeTests.XamarinForms.iOSBaseline");
            _testLogic.AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net6.0-ios");
        }


      
    }
}
