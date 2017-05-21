using System;
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
        private readonly string [] PropertiesNotNeededInCPS = new[] { "ProjectGuid", "ProjectTypeGuid" };

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

            var projectStyle = ProjectLoader.GetProjectStyle(_projectRootElement);

            switch (projectStyle)
            {
                case ProjectStyle.Default:
                    foreach (var import in _projectRootElement.Imports)
                    {
                        _projectRootElement.RemoveChild(import);
                    }
                    _projectRootElement.Sdk = "Microsoft.NET.Sdk";
                    break;
                case ProjectStyle.DefaultWithCustomTargets:
                    break;
                case ProjectStyle.Custom:
                    throw new NotSupportedException("Projects with more than 2 imports of custom targets are not supported");
            }

            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                // TODO: Handle prop groups with conditions - esp configurations.
                if (propGroup.Condition != "")
                {
                    continue;
                }

                foreach (var prop in propGroup.Properties)
                {
                    if (prop.Name == "OutputType")
                    {
                        var targetFrameworkElement = _projectRootElement.CreatePropertyElement("TargetFramework");
                        targetFrameworkElement.Value = _sdkBaselineProject.GetProperty("TargetFramework").EvaluatedValue;
                        propGroup.InsertBeforeChild(targetFrameworkElement, prop);
                        // Output path was added to the baseline - so it'll be defaulted but don't remove it.
                        continue;
                    }

                    if (propDiff.DefaultedProperties.Select(p => p.Name).Contains(prop.Name) ||
                        PropertiesNotNeededInCPS.Contains(prop.Name))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }

                if (propGroup.Properties.Count == 0)
                {
                    _projectRootElement.RemoveChild(propGroup);
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

                if (itemGroup.Items.Count == 0)
                {
                    _projectRootElement.RemoveChild(itemGroup);
                }
            }

            _projectRootElement.ToolsVersion = null;
            _projectRootElement.Save(outputProjectPath);
        }
    }
}
