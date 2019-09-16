using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Facts;
using Microsoft.Build.Construction;
using MSBuildAbstractions;
using PackageConversion;

namespace Conversion
{
    public class Converter
    {
        private readonly UnconfiguredProject _project;
        private readonly BaselineProject _sdkBaselineProject;
        private readonly IProjectRootElement _projectRootElement;
        private readonly ImmutableDictionary<string, Differ> _differs;
        private readonly DirectoryInfo _projectRootDirectory;

        public Converter(UnconfiguredProject project, BaselineProject sdkBaselineProject, IProjectRootElement projectRootElement, DirectoryInfo projectRootDirectory)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject;
            _projectRootElement = projectRootElement ?? throw new ArgumentNullException(nameof(projectRootElement));
            _projectRootDirectory = projectRootDirectory;
            _differs = GetDiffers();
        }

        public void Convert(string outputPath)
        {
            GenerateProjectFile();
            _projectRootElement.Save(outputPath);
        }

        internal IProjectRootElement GenerateProjectFile()
        {
            ChangeImports();

            RemoveDefaultedProperties();
            RemoveUnnecessaryPropertiesNotInSDKByDefault();

            var tfm = AddTargetFrameworkProperty();
            AddGenerateAssemblyInfo();
            AddDesktopProperties();

            AddTargetProjectProperties();

            AddConvertedPackages(tfm);
            RemoveOrUpdateItems(tfm);
            AddItemRemovesForIntroducedItems();

            ModifyProjectElement();

            return _projectRootElement;
        }

        internal ImmutableDictionary<string, Differ> GetDiffers() =>
            _project.ConfiguredProjects.Select(p => (p.Key, new Differ(p.Value, _sdkBaselineProject.Project.ConfiguredProjects[p.Key]))).ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Item2);

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
                    _projectRootElement.Sdk = DesktopFacts.WinSDKAttribute;
                }
                else
                {
                    _projectRootElement.Sdk = DesktopFacts.WinSDKAttribute;
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

        private void RemoveUnnecessaryPropertiesNotInSDKByDefault()
        {
            foreach (var propGroup in _projectRootElement.PropertyGroups)
            {
                foreach (var prop in propGroup.Properties)
                {
                    if (MSBuildFacts.UnnecessaryProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (MSBuildUtilities.IsDefineConstantDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (MSBuildUtilities.IsDebugTypeDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (MSBuildUtilities.IsOutputPathDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (MSBuildUtilities.IsPlatformTargetDefault(prop))
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

        private void RemoveOrUpdateItems(string tfm)
        {
            void UpdateBasedOnDiff(ImmutableArray<ItemsDiff> itemsDiff, ProjectItemGroupElement itemGroup, ProjectItemElement item)
            {
                var itemTypeDiff = itemsDiff.FirstOrDefault(id => id.ItemType.Equals(item.ItemType, StringComparison.OrdinalIgnoreCase));
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

            foreach (var itemGroup in _projectRootElement.ItemGroups)
            {
                var configurationName = MSBuildUtilities.GetConfigurationName(itemGroup.Condition);
                var itemsDiff = _differs[configurationName].GetItemsDiff();

                foreach (var item in itemGroup.Items.Where(item => !MSBuildUtilities.IsPackageReference(item)))
                {
                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (MSBuildUtilities.IsExplicitValueTupleReferenceNeeded(item, tfm))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (MSBuildUtilities.IsLegacyXamlDesignerItem(item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (_sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop && MSBuildUtilities.IsDependentUponXamlDesignerItem(item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (_sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop && MSBuildUtilities.IsWinFormsUIDesignerFile(item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (_sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop && MSBuildUtilities.DesktopReferencesNeedsRemoval(item))
                    {
                        // Desktop projects will only convert to .NET Core, so any item includes that have .NET Core equivalents will be removed.
                        // Users will have to ensure those packages are also added if they cannot do so with a tool.
                        // References that are already present will also be removed.
                        itemGroup.RemoveChild(item);
                    }
                    else
                    {
                        UpdateBasedOnDiff(itemsDiff, itemGroup, item);
                    }
                }

                if (itemGroup.Items.Count == 0)
                {
                    _projectRootElement.RemoveChild(itemGroup);
                }
            }
        }

        private void AddItemRemovesForIntroducedItems()
        {
            var introducedItems = _differs.Values
                                          .SelectMany(
                                                differ => differ.GetItemsDiff()
                                                                .Where(diff => MSBuildFacts.GlobbedItemTypes.Contains(diff.ItemType, StringComparer.OrdinalIgnoreCase))
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

        private void AddConvertedPackages(string tfm)
        {
            var packagesConfigItemGroups = MSBuildUtilities.GetPackagesConfigItemGroup(_projectRootElement);
            if (!packagesConfigItemGroups.Any())
            {
                return;
            }

            var packagesConfigItemGroup = packagesConfigItemGroups.Single();

            var packagesConfigItem = MSBuildUtilities.GetPackagesConfigItem(packagesConfigItemGroup);
            var path = Path.Combine(_projectRootDirectory.FullName, packagesConfigItem.Include);
            
            var packageReferences = PackagesConfigConverter.Convert(path);
            if (packageReferences is object && packageReferences.Any())
            {
                var groupForPackageRefs = _projectRootElement.AddItemGroup();
                foreach (var pkgref in packageReferences)
                {
                    if (pkgref.ID.Equals(MSBuildFacts.SystemValueTupleName, StringComparison.OrdinalIgnoreCase) && MSBuildUtilities.FrameworkHasAValueTuple(tfm))
                    {
                        continue;
                    }

                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(pkgref.ID, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // TODO: more metadata
                    var metadata = new List<KeyValuePair<string, string>>()
                    {
                        new KeyValuePair<string, string>("Version", pkgref.Version)
                    };

                    // TODO: some way to make Version not explicitly metadata
                    var item = groupForPackageRefs.AddItem(PackageFacts.PackageReferenceItemType, pkgref.ID, metadata);
                }

                // If the only references we had are already in the SDK, we're done.
                if (!groupForPackageRefs.Items.Any())
                {
                    _projectRootElement.RemoveChild(groupForPackageRefs);
                }
            }

            packagesConfigItemGroup.RemoveChild(packagesConfigItem);
        }

        private string AddTargetFrameworkProperty()
        {
            static string StripDecimals(string tfm)
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
                targetFrameworkElement.Value = Facts.MSBuildFacts.NETCoreDesktopTFM;
            }
            else
            {
                var rawTFM = _sdkBaselineProject.Project.FirstConfiguredProject.GetProperty("TargetFramework").EvaluatedValue;

                // This is pretty much never gonna happen, but it was cheap to write the code
                targetFrameworkElement.Value = MSBuildUtilities.IsNotNetFramework(rawTFM) ? StripDecimals(rawTFM) : rawTFM;
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
            var propGroup = MSBuildUtilities.GetTopPropertyGroupWithTFM(_projectRootElement);

            if (!_sdkBaselineProject.GlobalProperties.Contains(DesktopFacts.UseWinFormsPropertyName, StringComparer.OrdinalIgnoreCase) && MSBuildUtilities.IsWinForms(_projectRootElement))
            {
                var useWinForms = _projectRootElement.CreatePropertyElement(DesktopFacts.UseWinFormsPropertyName);
                useWinForms.Value = "true";
                propGroup.AppendChild(useWinForms);
            }

            if (!_sdkBaselineProject.GlobalProperties.Contains(DesktopFacts.UseWPFPropertyName, StringComparer.OrdinalIgnoreCase) && MSBuildUtilities.IsWPF(_projectRootElement))
            {
                var useWPF = _projectRootElement.CreatePropertyElement(DesktopFacts.UseWPFPropertyName);
                useWPF.Value = "true";
                propGroup.AppendChild(useWPF);
            }
        }

        private void AddGenerateAssemblyInfo()
        {
            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = MSBuildUtilities.GetTopPropertyGroupWithTFM(_projectRootElement);
            var generateAssemblyInfo = _projectRootElement.CreatePropertyElement(MSBuildFacts.GenerateAssemblyInfoNodeName);
            generateAssemblyInfo.Value = "false";
            propGroup.AppendChild(generateAssemblyInfo);
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

                var firstImport = _projectRootElement.Imports.Where(i => i.Label != MSBuildFacts.SharedProjectsImportLabel).First();
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
