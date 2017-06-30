using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Construction;

namespace ProjectSimplifier
{
    internal class Converter
    {
        private readonly UnconfiguredProject _project;
        private readonly BaselineProject _sdkBaselineProject;
        private readonly IProjectRootElement _projectRootElement;
        private readonly ImmutableDictionary<string, Differ> _differs;

        public Converter(UnconfiguredProject project, BaselineProject sdkBaselineProject, IProjectRootElement projectRootElement)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject;
            _projectRootElement = projectRootElement ?? throw new ArgumentNullException(nameof(projectRootElement));
            _differs = _project.ConfiguredProjects.Select(p => (p.Key, new Differ(p.Value, _sdkBaselineProject.Project.ConfiguredProjects[p.Key]))).ToImmutableDictionary(kvp => kvp.Item1, kvp => kvp.Item2);
        }

        internal void GenerateProjectFile(string outputProjectPath)
        {
            ChangeImports();

            RemoveDefaultedProperties();
            AddTargetFrameworkProperty();

            RemoveOrUpdateItems();

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

        private void RemoveDefaultedProperties()
        {
            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                var configurationName = MSBuildUtilities.GetConfigurationName(propGroup.Condition);
                var propDiff = _differs[configurationName].GetPropertiesDiff();

                foreach (var prop in propGroup.Properties)
                {
                    // These properties were added to the baseline - so don't treat them as defaulted properties.
                    if (_sdkBaselineProject.GlobalProperties.Contains(prop.Name))
                    {
                        continue;
                    }

                    if (propDiff.DefaultedProperties.Select(p => p.Name).Contains(prop.Name) ||
                        Facts.PropertiesNotNeededInCPS.Contains(prop.Name))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }

                // If a propertyGroup is empty we can remove it unless it had a condition from which a configuration is inferred.
                if (propGroup.Properties.Count == 0 && string.IsNullOrEmpty(configurationName))
                {
                    _projectRootElement.RemoveChild(propGroup);
                }
            }
        }

        private void RemoveOrUpdateItems()
        {
            foreach (var itemGroup in _projectRootElement.ItemGroups)
            {
                var configurationName = MSBuildUtilities.GetConfigurationName(itemGroup.Condition);
                var itemsDiff = _differs[configurationName].GetItemsDiff();

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

                    if(!itemTypeDiff.ChangedItems.IsDefault)
                    {
                        var changedItems = itemTypeDiff.ChangedItems.Select(i => i.EvaluatedInclude);
                        if (changedItems.Contains(item.Include))
                        {
                            var path = item.Include;
                            item.Include = null;
                            item.Update = path;
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
            if (_sdkBaselineProject.GlobalProperties.Contains("TargetFramework"))
            {
                // The original project had a TargetFramework property. No need to add it again.
                return;
            }

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
