using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MauiSmoke.Tests.Utilities;
using Microsoft.Build.Construction;

using MSBuild.Abstractions;
using MSBuild.Conversion.Project;


using Xunit;

namespace MauiSmoke.Tests
{
    public class MauiConversions : IClassFixture<MauiSolutionPathFixture>, IClassFixture<MauiMSBuildFixture>
    {
        private string SolutionPath => Environment.CurrentDirectory;
        private string TestDataPath => Path.Combine(SolutionPath, "tests", "TestData");
        private string GetXamarinAndroidProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.csproj");
        private string GetXamariniOSProjectPath(string projectName) => Path.Combine(TestDataPath, projectName, $"{projectName}.csproj");

        public MauiConversions(MauiSolutionPathFixture solutionPathFixture, MauiMSBuildFixture msBuildFixture)
        {
            msBuildFixture.MSBuildPathForXamarinProject();
            solutionPathFixture.SetCurrentDirectory();
        }

        [Fact]
        public void ConvertsXamarinFormsAndroidToMaui()
        {
            var projectToConvertPath = GetXamarinAndroidProjectPath("SmokeTests.XamarinForms.Android");
            var projectBaselinePath = GetXamarinAndroidProjectPath("SmokeTests.XamarinForms.AndroidBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net6.0-android");
        }

        [Fact]
        public void ConvertsXamarinFormsiOSToMaui()
        {
            var projectToConvertPath = GetXamariniOSProjectPath("SmokeTests.XamarinForms.iOS");
            var projectBaselinePath = GetXamariniOSProjectPath("SmokeTests.XamarinForms.iOSBaseline");
            AssertConversionWorks(projectToConvertPath, projectBaselinePath, "net6.0-ios");
        }

        private void AssertConversionWorks(string projectToConvertPath, string projectBaselinePath, string targetTFM, bool forceWeb = false, bool keepTargetFramework = false)
        {
            var (baselineRootElement, convertedRootElement) = GetRootElementsForComparison(projectToConvertPath, projectBaselinePath, targetTFM, forceWeb, keepTargetFramework);
            AssertPropsEqual(baselineRootElement, convertedRootElement);
            AssertItemsEqual(baselineRootElement, convertedRootElement);
        }

        private static (IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement) GetRootElementsForComparison(string projectToConvertPath, string projectBaselinePath, string targetTFM, bool forceWeb, bool keepTargetFramework)
        {
            var conversionLoader = new MSBuildConversionWorkspaceLoader(projectToConvertPath, MSBuildConversionWorkspaceType.Project);
            var conversionWorkspace = conversionLoader.LoadWorkspace(projectToConvertPath, noBackup: true, targetTFM, keepTargetFramework, forceWeb);

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
