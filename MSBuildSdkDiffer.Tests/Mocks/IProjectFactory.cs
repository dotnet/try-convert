using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Moq;

namespace MSBuildSdkDiffer.Tests.Mocks
{
    internal class IProjectFactory
    {
        /// <summary>
        /// Expected format here is "A=B;C=D"
        /// </summary>
        public static IProject Create(string projectProperties)
        {
            var lines = projectProperties.Split(';');
            return Create(lines.Select(p => p.Split('=')).ToDictionary(a => a[0], a => a[1]));
        }

        public static IProject Create(IDictionary<string, string> projectProperties)
        {
            var mock = new Mock<IProject>();

            mock.Setup(m => m.GetPropertyValue(It.IsAny<string>())).Returns((string prop) => projectProperties.ContainsKey(prop) ? projectProperties[prop] : "");

            mock.Setup(m => m.GetProperty(It.IsAny<string>())).Returns((string prop) => 
            {
                if (projectProperties.ContainsKey(prop))
                {
                    var projectProperty = new Mock<IProjectProperty>();
                    projectProperty.SetupGet(pp => pp.Name).Returns(prop);
                    projectProperty.SetupGet(pp => pp.EvaluatedValue).Returns(projectProperties[prop]);
                    return projectProperty.Object;
                }
                return null;
            });
            return mock.Object;
        }
    }
}
