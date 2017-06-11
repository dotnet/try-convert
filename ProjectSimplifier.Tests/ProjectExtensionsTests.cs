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
        [InlineData("net46", null, null, null, "net46")]
        [InlineData(null, ".NETFramework", "v4.6", null, "net4.6")]
        [InlineData(null, ".NETCoreApp", "v1.0", null, "netcoreapp1.0")]
        [InlineData(null, ".NETStandard", "v1.0", null, "netstandard1.0")]
        [InlineData(null, ".NETPortable", "v5.0", "Profile7", "netstandard1.1")]
        public void GetTargetFramework(string targetFramework, string targetFrameworkIdentifier, string targetFrameworkVersion, string targetFrameworkProfile, string expectedTargetFramework)
        {
            var properties = new Dictionary<string, string>();
            if (targetFramework != null)
                properties.Add("TargetFramework", targetFramework);
            if (targetFrameworkIdentifier != null)
                properties.Add("TargetFrameworkIdentifier", targetFrameworkIdentifier);
            if (targetFrameworkVersion != null)
                properties.Add("TargetFrameworkVersion", targetFrameworkVersion);
            if (targetFrameworkProfile != null)
                properties.Add("TargetFrameworkProfile", targetFrameworkProfile);

            var project = IProjectFactory.Create(properties);
            var actualTargetFramework = ProjectExtensions.GetTargetFramework(project);

            Assert.Equal(expectedTargetFramework, actualTargetFramework);
        }

        [Theory]
        [InlineData(null, null, "v4.6")]
        [InlineData(null, ".NETCoreApp", null)]
        [InlineData(null, "Unknown", null)]
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
