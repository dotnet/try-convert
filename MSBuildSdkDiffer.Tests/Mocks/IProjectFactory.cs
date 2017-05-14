using System.Collections.Generic;
using System.Linq;
using Moq;

namespace MSBuildSdkDiffer.Tests.Mocks
{
    internal class IProjectFactory
    {
        /// <summary>
        /// Expected format here is "A=B;C=D"
        /// </summary>
        public static IProject Create(string projectProperties, string propertiesInFile="")
        {
            var lines = projectProperties.Split(';');
            return Create(lines.Select(p => p.Split('=')).ToDictionary(a => a[0], a => a[1]), propertiesInFile.Split(';'));
        }

        public static IProject Create(IEnumerable<(string ItemType, string[] Items)> items)
        {
            var mock = new Mock<IProject>();

            var projectItems = new List<IProjectItem>();

            foreach (var itemGroup in items)
            {
                foreach (var item in itemGroup.Items)
                {
                    var projectItemMock = new Mock<IProjectItem>();
                    projectItemMock.SetupGet(pi => pi.ItemType).Returns(itemGroup.ItemType);
                    projectItemMock.SetupGet(pi => pi.EvaluatedInclude).Returns(item);
                    projectItems.Add(projectItemMock.Object);
                }
            }

            mock.SetupGet(m => m.Items).Returns(projectItems);

            return mock.Object;
        }

        public static IProject Create(IDictionary<string, string> projectProperties, IEnumerable<string> propertiesInFile=null)
        {
            var mock = new Mock<IProject>();

            mock.Setup(m => m.GetPropertyValue(It.IsAny<string>())).Returns((string prop) => projectProperties.ContainsKey(prop) ? projectProperties[prop] : "");

            mock.Setup(m => m.GetProperty(It.IsAny<string>())).Returns((string prop) => 
            {
                if (projectProperties.ContainsKey(prop))
                {
                    return MockProperty(prop, projectProperties[prop], propertiesInFile?.Contains(prop));
                }
                return null;
            });

            mock.SetupGet(m => m.Properties).Returns(projectProperties.Select(kvp => MockProperty(kvp.Key, kvp.Value, propertiesInFile?.Contains(kvp.Key))).ToArray());

            return mock.Object;
        }

        private static IProjectProperty MockProperty(string propName, string propValue, bool? isDefinedInProject)
        {
            var projectProperty = new Mock<IProjectProperty>();
            projectProperty.SetupGet(pp => pp.Name).Returns(propName);
            projectProperty.SetupGet(pp => pp.EvaluatedValue).Returns(propValue);
            projectProperty.SetupGet(pp => pp.IsDefinedInProject).Returns(isDefinedInProject??false);
            return projectProperty.Object;
        }
    }
}
