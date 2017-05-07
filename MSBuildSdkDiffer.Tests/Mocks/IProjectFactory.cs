using System.Collections.Generic;
using Microsoft.Build.Evaluation;
using Moq;

namespace MSBuildSdkDiffer.Tests.Mocks
{
    internal class IProjectFactory
    {
        public static IProject Create(IDictionary<string, string> projectProperties)
        {
            var mock = new Mock<IProject>();

            mock.Setup(m => m.GetPropertyValue(It.IsAny<string>())).Returns((string prop) => projectProperties.ContainsKey(prop) ? projectProperties[prop] : "");

            return mock.Object;
        }
    }
}
