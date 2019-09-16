using Conversion;
using Microsoft.Build.Construction;
using MSBuildAbstractions;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace SmokeTests
{
    public class BasicSmokeTests
    {
        [Fact]
        public void ConvertsLegacyFSharpConsole()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin");
            var projectToConvertPath = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.LegacyFSharpConsole", "SmokeTests.LegacyFSharpConsole.fsproj");
            var projectBaselinePath = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.FSharpConsoleBaseline", "SmokeTests.FSharpConsoleBaseline.fsproj");

            var (baselineRootElement, convertedRootElement) = GetRootElementsForComparison(projectToConvertPath, projectBaselinePath);

            AssertPropsEqual(baselineRootElement, convertedRootElement);
        }

        [Fact]
        public void ConvertsWpfFrameworkTemplate()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin");
            var projectToConvertPath = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.WpfFramework", "SmokeTests.WpfFramework.csproj");
            var projectBaselinePath = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.WpfCoreBaseline", "SmokeTests.WpfCoreBaseline.csproj");

            // TODO
            Assert.True(true);
        }

        [Fact]
        public void ConvertsWinformsFrameworkTemplate()
        {
            MSBuildUtilities.HookAssemblyResolveForMSBuild(@"C:\Program Files (x86)\Microsoft Visual Studio\2019\Preview\MSBuild\Current\Bin");
            var loader = new ProjectLoader();
            var path = Path.Combine("..", "..", "..", "..", "TestData", "SmokeTests.WinformsFramework", "SmokeTests.WinformsFramework.csproj");
            loader.LoadProjects(path);
            var converter = new Converter(loader.Project, loader.SdkBaselineProject, loader.ProjectRootElement, loader.ProjectRootDirectory);
            var str = converter.GenerateProjectFile();
            Assert.True(true);
        }

        private void AssertPropsEqual(IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement)
        {
            Assert.Equal(baselineRootElement.Sdk, convertedRootElement.Sdk);
            Assert.Equal(baselineRootElement.PropertyGroups.Count, convertedRootElement.PropertyGroups.Count);

            var baselinePropGroups = new List<ProjectPropertyGroupElement>(baselineRootElement.PropertyGroups);
            var convertedPropGroups = new List<ProjectPropertyGroupElement>(convertedRootElement.PropertyGroups);

            for (int i = 0; i < baselinePropGroups.Count; i++)
            {
                var baselineProps = new List<ProjectPropertyElement>(baselinePropGroups[i].Properties);
                var convertedProps = new List<ProjectPropertyElement>(convertedPropGroups[i].Properties);

                for (int j = 0; j < baselineProps.Count; j++)
                {
                    var baselineProp = baselineProps[j];
                    var convertedProp = convertedProps[j];

                    Assert.Equal(baselineProp.Name, convertedProp.Name);
                    Assert.Equal(baselineProp.Value, convertedProp.Value);
                }
            }
        }

        private static (IProjectRootElement baselineRootElement, IProjectRootElement convertedRootElement) GetRootElementsForComparison(string projectToConvertPath, string projectBaselinePath)
        {
            var conversionLoader = new ProjectLoader();
            conversionLoader.LoadProjects(projectToConvertPath);

            var baselineLoader = new ProjectLoader();
            var baselineRootElement = baselineLoader.GetRootElementFromProjectFile(projectBaselinePath);

            var converter = new Converter(conversionLoader.Project, conversionLoader.SdkBaselineProject, conversionLoader.ProjectRootElement, conversionLoader.ProjectRootDirectory);
            var convertedRootElement = converter.GenerateProjectFile();

            return (baselineRootElement, convertedRootElement);
        }
    }
}
