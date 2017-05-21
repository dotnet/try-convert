using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Construction;

namespace MSBuildSdkDiffer
{
    internal class Converter
    {
        private readonly MSBuildProject _project;
        private readonly MSBuildProject _sdkBaselineProject;
        private readonly ProjectRootElement _projectRootElement;
        private readonly Differ _differ;

        public Converter(MSBuildProject project, MSBuildProject sdkBaselineProject, ProjectRootElement projectRootElement)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject ?? throw new ArgumentNullException(nameof(sdkBaselineProject));
            _projectRootElement = projectRootElement ?? throw new ArgumentNullException(nameof(projectRootElement));
            _differ = new Differ(_project, _sdkBaselineProject);
        }

        internal void GenerateProjectFile(string outputProjectPath)
        {
            var propDiff = _differ.GetPropertiesDiff();
            var itemsDiff = _differ.GetItemsDiff();
            
            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                // TODO: Handle prop groups with conditions - esp configurations.
                if (propGroup.Condition != "")
                {
                    continue;
                }

                foreach (var prop in propGroup.Properties)
                {
                    if (propDiff.DefaultedProperties.Select(p => p.Name).Contains(prop.Name))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }
            }

            foreach (var itemGroup in _projectRootElement.ItemGroups)
            {
                foreach (var item in itemGroup.Items)
                {
                    ItemsDiff itemTypeDiff = itemsDiff.FirstOrDefault(id => id.ItemType == item.ItemType);
                    if (!itemTypeDiff.DefaultedItems.IsDefault)
                    {
                        var defaultedItems = itemTypeDiff.DefaultedItems.Select(i => i.EvaluatedInclude);
                        if (defaultedItems.Contains(item.Include))
                        {
                            itemGroup.RemoveChild(item);
                        }
                    }
                }
            }

            _projectRootElement.Save(outputProjectPath);
        }
    }
}
