using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Construction;

namespace MSBuildSdkDiffer
{
    internal class Converter
    {
        private readonly UnconfiguredProject _project;
        private readonly BaselineProject _sdkBaselineProject;
        private readonly ProjectRootElement _projectRootElement;
        private readonly Differ _differ;
        private readonly string [] PropertiesNotNeededInCPS = new[] { "ProjectGuid", "ProjectTypeGuid" };

        public Converter(UnconfiguredProject project, BaselineProject sdkBaselineProject, ProjectRootElement projectRootElement)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject;
            _projectRootElement = projectRootElement ?? throw new ArgumentNullException(nameof(projectRootElement));
            _differ = new Differ(_project.FirstConfiguredProject, _sdkBaselineProject.Project.FirstConfiguredProject);
        }

        internal void GenerateProjectFile(string outputProjectPath)
        {
            var propDiff = _differ.GetPropertiesDiff();
            var itemsDiff = _differ.GetItemsDiff();

            ChangeImports();

            RemoveDefaultedProperties(propDiff);
            AddTargetFrameworkProperty();

            RemoveDefaultedItems(itemsDiff);

            _projectRootElement.ToolsVersion = null;
            _projectRootElement.Save(outputProjectPath);
        }

        private void ChangeImports()
        {
            var projectStyle = _sdkBaselineProject.ProjectStyle;

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
        }

        private void RemoveDefaultedProperties(PropertiesDiff propDiff)
        {
            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                // TODO: Handle prop groups with conditions - esp configurations.
                if (propGroup.Condition != "")
                {
                    continue;
                }

                foreach (var prop in propGroup.Properties)
                {
                    // These properties were added to the baseline - so don't treat them as defaulted proeprties.
                    if (_sdkBaselineProject.GlobalProperties.Contains(prop.Name))
                    {
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
        }

        private void RemoveDefaultedItems(ImmutableArray<ItemsDiff> itemsDiff)
        {
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
        }

        private void AddTargetFrameworkProperty()
        {
            var propGroup = _projectRootElement.PropertyGroups.FirstOrDefault(pg => pg.Condition == "");
            if (propGroup == null)
            {
                propGroup = _projectRootElement.AddPropertyGroup();
            }

            var targetFrameworkElement = _projectRootElement.CreatePropertyElement("TargetFramework");
            targetFrameworkElement.Value = _sdkBaselineProject.Project.FirstConfiguredProject.GetProperty("TargetFramework").EvaluatedValue;
            propGroup.PrependChild(targetFrameworkElement);
        }
    }
}
