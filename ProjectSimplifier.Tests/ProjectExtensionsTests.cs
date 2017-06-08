using System;
using System.Collections.Generic;
using System.Linq;
using ProjectSimplifier;
using ProjectSimplifier.Tests.Mocks;
using Xunit;

namespace ProjectSimplifier.Tests
{
    public class ProjectExtensionsTests
    {
        [Theory]
        [InlineData("net46", null, null, "net46")]
        [InlineData(null, ".NETFramework", "v4.6", "net4.6")]
        [InlineData(null, ".NETCoreApp", "v1.0", "netcoreapp1.0")]
        [InlineData(null, ".NETStandard", "v1.0", "netstandard1.0")]
        public void GetTargetFramework(string targetFramework, string targetFrameworkIdentifier, string targetFrameworkVersion, string expectedTargetFramework)
        {
            var properties = new Dictionary<string, string>();
            if (targetFramework != null)
                properties.Add("TargetFramework", targetFramework);
            if (targetFrameworkIdentifier != null)
                properties.Add("TargetFrameworkIdentifier", targetFrameworkIdentifier);
            if (targetFrameworkVersion != null)
                properties.Add("TargetFrameworkVersion", targetFrameworkVersion);

            var project = IProjectFactory.Create(properties);
            var actualTargetFramework = ProjectExtensions.GetTargetFramework(project);

            Assert.Equal(expectedTargetFramework, actualTargetFramework);
        }

        [Theory]
        [InlineData(null, null, "v4.6")]
        [InlineData(null, ".NETCoreApp", null)]
        public void GetTargetFramework_Throws(string targetFramework, string targetFrameworkIdentifier, string targetFrameworkVersion)
        {
            var properties = new Dictionary<string, string>();
            if (targetFramework != null)
                properties.Add("TargetFramework", targetFramework);
            if (targetFrameworkIdentifier != null)
                properties.Add("TargetFrameworkIdentifier", targetFrameworkIdentifier);
            if (targetFrameworkVersion != null)
                properties.Add("TargetFrameworkVersion", targetFrameworkVersion);

            var project = IProjectFactory.Create(properties);
            
            Assert.Throws<InvalidOperationException>(() => ProjectExtensions.GetTargetFramework(project));
        }

    }
}
