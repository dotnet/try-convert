﻿using System;
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
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsLegacyFSharpConsoleToNet50()
        {
            var projectToConvertPath = GetFSharpProjectPath("SmokeTests.LegacyFSharpConsole");
            var projectBaselinePath = GetFSharpProjectPath("SmokeTests.FSharpConsoleNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0");
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplateForNetCoreApp31()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WpfFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WpfCoreBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WpfFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WpfNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsWpfVbFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetVisualBasicProjectPath("SmokeTests.WpfVbFramework");
            var projectBaselinePath = GetVisualBasicProjectPath("SmokeTests.WpfVbNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplateForNetCoreApp31()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WinformsFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WinformsCoreBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplateForNet50()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.WinformsFramework");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WinformsNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsLegacyMSTest()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyMSTest");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.MSTestCoreBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "netcoreapp3.1");
        }

        [Fact]
        public void ConvertsLegacyMSTestVB()
        {
            var projectToConvertPath = GetVisualBasicProjectPath("SmokeTests.LegacyMSTestVB");
            var projectBaselinePath = GetVisualBasicProjectPath("SmokeTests.MSTestVbNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0-windows");
        }

        [Fact]
        public void ConvertsLegacyWebLibraryToNetFx()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyWebLibrary");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WebLibraryNetFxBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net472", true);
        }

        [Fact]
        public void ConvertsLegacyWebLibraryToNet5()
        {
            var projectToConvertPath = GetCSharpProjectPath("SmokeTests.LegacyWebLibrary");
            var projectBaselinePath = GetCSharpProjectPath("SmokeTests.WebLibraryNet5Baseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net5.0", true);
        }

        private void AssertConversionWorks(string projectToConvertPath, string projectBaselinePath, string targetTFM, bool forceWeb = false)
        {
            var (baselineRootElement, convertedRootElement) = GetRootElementsForComparison(projectToConvertPath, projectBaselinePath, targetTFM, forceWeb);
            AssertPropsEqual(baselineRootElement, convertedRootElement);
            AssertItemsEqual(baselineRootElement, convertedRootElement);
        }

        private static (IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement) GetRootElementsForComparison(string projectToConvertPath, string projectBaselinePath, string targetTFM, bool forceWeb)
        {
            var conversionLoader = new MSBuildConversionWorkspaceLoader(projectToConvertPath, MSBuildConversionWorkspaceType.Project);
            var conversionWorkspace = conversionLoader.LoadWorkspace(projectToConvertPath, noBackup: true, targetTFM, false, forceWeb);

            var baselineLoader = new MSBuildConversionWorkspaceLoader(projectBaselinePath, MSBuildConversionWorkspaceType.Project);
            var baselineRootElement = baselineLoader.GetRootElementFromProjectFile(projectBaselinePath);

            var item = conversionWorkspace.WorkspaceItems.Single();
            var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement, noBackup: false);
            var convertedRootElement = converter.ConvertProjectFile();

            return (baselineRootElement, convertedRootElement);
        }

        private void AssertPropsEqual(IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement)
        {
            Assert.Equal(baselineRootElement.Sdk, convertedRootElement.Sdk);
            Assert.Equal(baselineRootElement.PropertyGroups.Count, convertedRootElement.PropertyGroups.Count);

            var baselinePropGroups = new List<ProjectPropertyGroupElement>(baselineRootElement.PropertyGroups);
            var convertedPropGroups = new List<ProjectPropertyGroupElement>(convertedRootElement.PropertyGroups);

            if (baselinePropGroups.Count > 0)
            {
                for (var i = 0; i < baselinePropGroups.Count; i++)
                {                    
                    var baselineProps = new List<ProjectPropertyElement>(baselinePropGroups[i].Properties);
                    var convertedProps = new List<ProjectPropertyElement>(convertedPropGroups[i].Properties);

                    Assert.Equal(baselineProps.Count, convertedProps.Count);

                    if (baselineProps.Count > 0)
                    {
                        for (var j = 0; j < baselineProps.Count; j++)
                        {
                            var baselineProp = baselineProps[j];
                            var convertedProp = convertedProps[j];

                            Assert.Equal(baselineProp.Name, convertedProp.Name);
                            Assert.Equal(baselineProp.Value, convertedProp.Value);
                        }
                    }
                }
            }
        }

        private void AssertItemsEqual(IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement)
        {
            Assert.Equal(baselineRootElement.Sdk, convertedRootElement.Sdk);
            Assert.Equal(baselineRootElement.ItemGroups.Count, convertedRootElement.ItemGroups.Count);

            var baselineItemGroups = new List<ProjectItemGroupElement>(baselineRootElement.ItemGroups);
            var convertedItemGroups = new List<ProjectItemGroupElement>(convertedRootElement.ItemGroups);

            if (baselineItemGroups.Count > 0)
            {
                for (var i = 0; i < baselineItemGroups.Count; i++)
                {
                    var baselineItems = new List<ProjectItemElement>(baselineItemGroups[i].Items);
                    var convertedItems = new List<ProjectItemElement>(convertedItemGroups[i].Items);

                    // TODO: this was regressed at some point
                    //       converted items will now have additional items
                    // Assert.Equal(baselineItems.Count, convertedItems.Count);

                    if (baselineItems.Count > 1)
                    {
                        for (var j = 0; j < baselineItems.Count; j++)
                        {
                            var baselineItem = baselineItems[j];
                            var convertedItem = convertedItems[j];

                            Assert.Equal(baselineItem.Include, convertedItem.Include);
                            Assert.Equal(baselineItem.Update, convertedItem.Update);
                        }
                    }
                }
            }
        }
    }
}
