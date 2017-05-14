using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Construction;

namespace MSBuildSdkDiffer
{
    class Converter
    {
        private readonly MSBuildProject _project;
        private readonly List<string> _propertiesInFile;
        private readonly MSBuildProject _sdkBaselineProject;
        private readonly ProjectRootElement _rootElement;
        private readonly Differ _differ;

        public Converter(MSBuildProject project, List<string> propertiesInFile, MSBuildProject sdkBaselineProject, ProjectRootElement rootElement)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _propertiesInFile = propertiesInFile ?? throw new ArgumentNullException(nameof(propertiesInFile));
            _sdkBaselineProject = sdkBaselineProject ?? throw new ArgumentNullException(nameof(sdkBaselineProject));
            _rootElement = rootElement ?? throw new ArgumentNullException(nameof(rootElement));

            _differ = new Differ(_project, _propertiesInFile, _sdkBaselineProject);
        }

        internal void GenerateProjectFile(string outputProjectPath)
        {
            var propGroup = _rootElement.AddPropertyGroup();

            var propDiff = _differ.GetPropertiesDiff();
            foreach (var prop in propDiff.NotDefaultedProperties)
            {
                propGroup.AddProperty(prop.Name, prop.UnevaluatedValue);
            }

            foreach (var prop in propDiff.ChangedProperties)
            {
                propGroup.AddProperty(prop.oldProp.Name, prop.oldProp.UnevaluatedValue);
            }

            var itemGroup = _rootElement.AddItemGroup();

            _rootElement.Save(outputProjectPath);
        }
    }
}
