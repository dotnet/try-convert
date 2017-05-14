using System;
using System.Collections.Generic;
using Microsoft.Build.Construction;

namespace MSBuildSdkDiffer
{
    internal class Converter
    {
        private readonly MSBuildProject _project;
        private readonly MSBuildProject _sdkBaselineProject;
        private readonly Differ _differ;

        public Converter(MSBuildProject project, MSBuildProject sdkBaselineProject)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject ?? throw new ArgumentNullException(nameof(sdkBaselineProject));

            _differ = new Differ(_project, _sdkBaselineProject);
        }

        internal void GenerateProjectFile(string outputProjectPath)
        {
            var rootElement = ProjectRootElement.Open(_project.FullPath);
            var propGroup = rootElement.AddPropertyGroup();

            var propDiff = _differ.GetPropertiesDiff();
            foreach (var prop in propDiff.NotDefaultedProperties)
            {
                propGroup.AddProperty(prop.Name, prop.UnevaluatedValue);
            }

            foreach (var prop in propDiff.ChangedProperties)
            {
                propGroup.AddProperty(prop.oldProp.Name, prop.oldProp.UnevaluatedValue);
            }

            var itemGroup = rootElement.AddItemGroup();

            rootElement.Save(outputProjectPath);
        }
    }
}
