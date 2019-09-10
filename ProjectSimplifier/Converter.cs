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
            RemoveUnnecessaryProperties();

            var tfm = AddTargetFrameworkProperty();
            AddDesktopProperties();

            AddTargetProjectProperties();

            RemoveOrUpdateItems();
            RemoveUnnecessaryReferences(tfm);
            AddItemRemovesForIntroducedItems();

            ModifyProjectElement();

            _projectRootElement.Save(outputProjectPath);
            Console.WriteLine($"Successfully converted project to {outputProjectPath}");
        }

        private void ChangeImports()
        {
            var projectStyle = _sdkBaselineProject.ProjectStyle;

            if (projectStyle == ProjectStyle.Default || projectStyle == ProjectStyle.WindowsDesktop)
            {
                foreach (var import in _projectRootElement.Imports)
                {
                    _projectRootElement.RemoveChild(import);
                }

                if (MSBuildUtilities.IsWinForms(_projectRootElement) || MSBuildUtilities.IsWinForms(_projectRootElement))
                {
                    _projectRootElement.Sdk = Facts.WinSDKAttribute;
                }
                else
                {
                    _projectRootElement.Sdk = Facts.WinSDKAttribute;
                }
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
                    if (_sdkBaselineProject.GlobalProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (propDiff.DefaultedProperties.Select(p => p.Name).Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
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

        private void RemoveUnnecessaryProperties()
        {
            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                var configurationName = MSBuildUtilities.GetConfigurationName(propGroup.Condition);

                foreach (var prop in propGroup.Properties)
                {
                    if (Facts.UnnecessaryProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                    }

                    if (propGroup.Properties.Count == 0)
                    {
                        _projectRootElement.RemoveChild(propGroup);
                    }
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
                    ItemsDiff itemTypeDiff = itemsDiff.FirstOrDefault(id => id.ItemType.Equals(item.ItemType, StringComparison.OrdinalIgnoreCase));
                    if (!itemTypeDiff.DefaultedItems.IsDefault)
                    {
                        var defaultedItems = itemTypeDiff.DefaultedItems.Select(i => i.EvaluatedInclude);
                        if (defaultedItems.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                        {
                            itemGroup.RemoveChild(item);
                        }
                    }

                    if (!itemTypeDiff.ChangedItems.IsDefault)
                    {
                        var changedItems = itemTypeDiff.ChangedItems.Select(i => i.EvaluatedInclude);
                        if (changedItems.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
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

        private void RemoveUnnecessaryReferences(string tfm)
        {
            bool DesktopItemNeedsRemoval(ProjectItemElement item)
            {
                return Facts.ItemsWithPackagesThatWorkOnNETCore.Contains(item.Include, StringComparer.OrdinalIgnoreCase) ||
                       Facts.DesktopReferencesThatNeedRemoval.Contains(item.Include, StringComparer.OrdinalIgnoreCase);
            }

            foreach (var itemGroup in _projectRootElement.ItemGroups)
            {
                foreach (var item in MSBuildUtilities.GetApplicableItems(itemGroup))
                {
                    if (Facts.UnnecessaryItemIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        itemGroup.RemoveChild(item);
                    }

                    if (item.Include.Equals("System.ValueTuple", StringComparison.OrdinalIgnoreCase) && MSBuildUtilities.FrameworkHasAValueTuple(tfm))
                    {
                        itemGroup.RemoveChild(item);
                    }

                    // Desktop projects will only convert to .NET Core, so any item includes that have .NET Core equivalents will be removed.
                    // Users will have to ensure those packages are also added if they cannot do so with a tool.
                    if (_sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop && DesktopItemNeedsRemoval(item))
                    {
                        itemGroup.RemoveChild(item);
                    }

                    if (itemGroup.Count == 0)
                    {
                        _projectRootElement.RemoveChild(itemGroup);
                    }
                }
            }
        }

        private void AddItemRemovesForIntroducedItems()
        {
            var introducedItems = _differs.Values
                                          .SelectMany(
                                                differ => differ.GetItemsDiff()
                                                                .Where(diff => Facts.GlobbedItemTypes.Contains(diff.ItemType, StringComparer.OrdinalIgnoreCase))
                                                                .SelectMany(diff => diff.IntroducedItems))
                                          .Distinct(ProjectItemComparer.IncludeComparer);

            if (introducedItems.Any())
            {
                var itemGroup = _projectRootElement.AddItemGroup();
                foreach (var introducedItem in introducedItems)
                {
                    var item = itemGroup.AddItem(introducedItem.ItemType, introducedItem.EvaluatedInclude);
                    item.Include = null;
                    item.Remove = introducedItem.EvaluatedInclude;
                }
            }
        }

        private string AddTargetFrameworkProperty()
        {
            string StripDecimals(string tfm)
            {
                var parts = tfm.Split('.');
                return string.Join("", parts);
            }

            if (_sdkBaselineProject.GlobalProperties.Contains("TargetFramework", StringComparer.OrdinalIgnoreCase))
            {
                // The original project had a TargetFramework property. No need to add it again.
                return _sdkBaselineProject.GlobalProperties.First(p => p.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase));
            }

            var propGroup = GetOrCreateEmptyPropertyGroup();

            var targetFrameworkElement = _projectRootElement.CreatePropertyElement("TargetFramework");

            if (_sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop)
            {
                targetFrameworkElement.Value = Facts.NETCoreDesktopTFM;
            }
            else
            {
                var rawTFM = _sdkBaselineProject.Project.FirstConfiguredProject.GetProperty("TargetFramework").EvaluatedValue;

                // This is pretty much never gonna happen, but it was cheap to write the code
                if (!rawTFM.ContainsIgnoreCase("netstandard", StringComparison.OrdinalIgnoreCase) && !rawTFM.ContainsIgnoreCase("netcoreapp", StringComparison.OrdinalIgnoreCase))
                {
                    targetFrameworkElement.Value = StripDecimals(rawTFM);
                }
                else
                {
                    targetFrameworkElement.Value = rawTFM;
                }
            }

            propGroup.PrependChild(targetFrameworkElement);

            return targetFrameworkElement.Value;
        }

        private void AddDesktopProperties()
        {
            if (_sdkBaselineProject.ProjectStyle != ProjectStyle.WindowsDesktop)
            {
                return;
            }

            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = _projectRootElement.PropertyGroups.Single(pg => pg.Children.Any(p => p.ElementName == "TargetFramework"));

            if (!_sdkBaselineProject.GlobalProperties.Contains(Facts.UseWinFormsPropertyName, StringComparer.OrdinalIgnoreCase) && MSBuildUtilities.IsWinForms(_projectRootElement))
            {
                var useWinForms = _projectRootElement.CreatePropertyElement(Facts.UseWinFormsPropertyName);
                useWinForms.Value = "true";
                propGroup.PrependChild(useWinForms);
            }

            if (!_sdkBaselineProject.GlobalProperties.Contains(Facts.UseWPFPropertyName, StringComparer.OrdinalIgnoreCase) && MSBuildUtilities.IsWPF(_projectRootElement))
            {
                var useWPF = _projectRootElement.CreatePropertyElement(Facts.UseWPFPropertyName);
                useWPF.Value = "true";
                propGroup.PrependChild(useWPF);
            }
        }

        private void ModifyProjectElement()
        {
            _projectRootElement.ToolsVersion = null;
            _projectRootElement.DefaultTargets = null;
        }

        private ProjectPropertyGroupElement GetOrCreateEmptyPropertyGroup()
        {
            bool IsAfterFirstImport(ProjectPropertyGroupElement propertyGroup)
            {
                if (_sdkBaselineProject.ProjectStyle == ProjectStyle.Default || _sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop)
                    return true;

                var firstImport = _projectRootElement.Imports.Where(i => i.Label != Facts.SharedProjectsImportLabel).First();
                return propertyGroup.Location.Line > firstImport.Location.Line;
            }

            return _projectRootElement.PropertyGroups.FirstOrDefault(pg => pg.Condition == "" &&
                                                                     IsAfterFirstImport(pg))
                    ?? _projectRootElement.AddPropertyGroup();
        }

        private void AddTargetProjectProperties()
        {
            if (_sdkBaselineProject.TargetProjectProperties.IsEmpty)
            {
                return;
            }

            var propGroup = GetOrCreateEmptyPropertyGroup();

            foreach (var prop in _sdkBaselineProject.TargetProjectProperties)
            {
                propGroup.AddProperty(prop.Key, prop.Value);
            }
        }
    }
}
