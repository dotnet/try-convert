using Facts;
using Microsoft.Build.Construction;
using MSBuildAbstractions;
using PackageConversion;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Conversion
{
    public static class ProjectRootElementExtensions
    {
        public static IProjectRootElement ChangeImports(this IProjectRootElement projectRootElement, BaselineProject baselineProject)
        {
            var projectStyle = baselineProject.ProjectStyle;

            if (projectStyle == ProjectStyle.Default || projectStyle == ProjectStyle.DefaultSubset || projectStyle == ProjectStyle.WindowsDesktop)
            {
                foreach (var import in projectRootElement.Imports)
                {
                    projectRootElement.RemoveChild(import);
                }

                if (MSBuildHelpers.IsWinForms(projectRootElement) || MSBuildHelpers.IsWPF(projectRootElement))
                {
                    projectRootElement.Sdk = DesktopFacts.WinSDKAttribute;
                }
                else
                {
                    projectRootElement.Sdk = MSBuildFacts.DefaultSDKAttribute;
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement RemoveDefaultedProperties(this IProjectRootElement projectRootElement, BaselineProject baselineProject, ImmutableDictionary<string, Differ> differs)
        {
            foreach (var propGroup in projectRootElement.PropertyGroups)
            {
                var configurationName = MSBuildHelpers.GetConfigurationName(propGroup.Condition);
                var propDiff = differs[configurationName].GetPropertiesDiff();

                foreach (var prop in propGroup.Properties)
                {
                    // These properties were added to the baseline - so don't treat them as defaulted properties.
                    if (baselineProject.GlobalProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
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
                    projectRootElement.RemoveChild(propGroup);
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement RemoveUnnecessaryPropertiesNotInSDKByDefault(this IProjectRootElement projectRootElement)
        {
            foreach (var propGroup in projectRootElement.PropertyGroups)
            {
                foreach (var prop in propGroup.Properties)
                {
                    if (MSBuildFacts.UnnecessaryProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDefineConstantDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDebugTypeDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsOutputPathDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsPlatformTargetDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsNameDefault(prop, GetProjectName(projectRootElement.FullPath)))
                    {
                        propGroup.RemoveChild(prop);
                    }
                    else if (ProjectPropertyHelpers.IsDocumentationFileDefault(prop))
                    {
                        propGroup.RemoveChild(prop);
                    }
                }

                if (propGroup.Properties.Count == 0)
                {
                    projectRootElement.RemoveChild(propGroup);
                }
            }

            return projectRootElement;

            static string GetProjectName(string projectPath)
            {
                var projName = projectPath.Split('\\').Last();
                return projName.Substring(0, projName.LastIndexOf('.'));
            }
        }

        public static IProjectRootElement RemoveOrUpdateItems(this IProjectRootElement projectRootElement, ImmutableDictionary<string, Differ> differs, BaselineProject baselineProject, string tfm)
        {
            foreach (var itemGroup in projectRootElement.ItemGroups)
            {
                var configurationName = MSBuildHelpers.GetConfigurationName(itemGroup.Condition);

                foreach (var item in itemGroup.Items.Where(item => !ProjectItemHelpers.IsPackageReference(item)))
                {
                    if (item.HasMetadata && ProjectItemHelpers.CanItemMetadataBeRemoved(item))
                    {
                        foreach (var metadataElement in item.Metadata)
                        {
                            item.RemoveChild(metadataElement);
                        }
                    }

                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(item.Include, StringComparer.OrdinalIgnoreCase))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsExplicitValueTupleReferenceThatCanBeRemoved(item, tfm))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsReferenceConvertibleToPackageReference(item))
                    {
                        var packageName = item.Include;
                        var version = MSBuildFacts.DefaultItemsThatHavePackageEquivalents[packageName];

                        projectRootElement.AddPackage(packageName, version);
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsReferenceComingFromOldNuGet(item))
                    {
                        // We've already converted it to PackageReference.
                        // Yes, this might mean their references are broken, since this might not have been convertable to PackageReference.
                        // That's probably fine!
                        itemGroup.RemoveChild(item);
                    }
                    else if (IsDesktopRemovableItem(baselineProject, itemGroup, item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else if (ProjectItemHelpers.IsItemWithUnnecessaryMetadata(item))
                    {
                        itemGroup.RemoveChild(item);
                    }
                    else
                    {
                        var itemsDiff = differs[configurationName].GetItemsDiff();
                        UpdateBasedOnDiff(itemsDiff, itemGroup, item);
                    }
                }

                if (itemGroup.Items.Count == 0)
                {
                    projectRootElement.RemoveChild(itemGroup);
                }
            }

            return projectRootElement;

            static void UpdateBasedOnDiff(ImmutableArray<ItemsDiff> itemsDiff, ProjectItemGroupElement itemGroup, ProjectItemElement item)
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

            static bool IsDesktopRemovableItem(BaselineProject sdkBaselineProject, ProjectItemGroupElement itemGroup, ProjectItemElement item)
            {
                return sdkBaselineProject.ProjectStyle == ProjectStyle.WindowsDesktop
                       && (ProjectItemHelpers.IsLegacyXamlDesignerItem(item)
                           || ProjectItemHelpers.IsDependentUponXamlDesignerItem(item)
                           || ProjectItemHelpers.IsDesignerFile(item)
                           || ProjectItemHelpers.IsSettingsFile(item)
                           || ProjectItemHelpers.IsResxFile(item)
                           || ProjectItemHelpers.DesktopReferencesNeedsRemoval(item)
                           || ProjectItemHelpers.IsDesktopRemovableGlobbedItem(sdkBaselineProject.ProjectStyle, item));
            }
        }


        public static IProjectRootElement AddPackage(this IProjectRootElement projectRootElement, string packageName, string packageVersion)
        {
            var packageReferencesItemGroup = MSBuildHelpers.GetOrCreatePackageReferencesItemGroup(projectRootElement);
            AddPackageReferenceElement(packageReferencesItemGroup, packageName, packageVersion);
            return projectRootElement;
        }

        public static IProjectRootElement AddConvertedPackages(this IProjectRootElement projectRootElement, string tfm)
        {
            var packagesConfigItemGroup = MSBuildHelpers.GetPackagesConfigItemGroup(projectRootElement);
            if (packagesConfigItemGroup is null)
            {
                return projectRootElement;
            }

            var packagesConfigItem = MSBuildHelpers.GetPackagesConfigItem(packagesConfigItemGroup);
            var path = Path.Combine(projectRootElement.DirectoryPath, packagesConfigItem.Include);

            var packageReferences = PackagesConfigConverter.Convert(path);
            if (packageReferences is { } && packageReferences.Any())
            {
                var groupForPackageRefs = projectRootElement.AddItemGroup();
                foreach (var pkgref in packageReferences)
                {
                    if (pkgref.ID.Equals(MSBuildFacts.SystemValueTupleName, StringComparison.OrdinalIgnoreCase) && MSBuildHelpers.FrameworkHasAValueTuple(tfm))
                    {
                        continue;
                    }

                    if (MSBuildFacts.UnnecessaryItemIncludes.Contains(pkgref.ID, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AddPackageReferenceElement(groupForPackageRefs, pkgref.ID, pkgref.Version);
                }

                // If the only references we had are already in the SDK, we're done.
                if (!groupForPackageRefs.Items.Any())
                {
                    projectRootElement.RemoveChild(groupForPackageRefs);
                }
            }

            packagesConfigItemGroup.RemoveChild(packagesConfigItem);
            return projectRootElement;
        }

        private static void AddPackageReferenceElement(ProjectItemGroupElement packageReferencesItemGroup, string packageName, string packageVersion)
        {
            var packageReference = packageReferencesItemGroup.AddItem(PackageFacts.PackageReferenceItemType, packageName);
            packageReference.GetXml().SetAttribute("Version", packageVersion);
        }

        public static IProjectRootElement AddDesktopProperties(this IProjectRootElement projectRootElement, BaselineProject baselineProject)
        {
            if (baselineProject.ProjectStyle != ProjectStyle.WindowsDesktop)
            {
                return projectRootElement;
            }

            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.UseWinFormsPropertyName, StringComparer.OrdinalIgnoreCase)
                && MSBuildHelpers.IsWinForms(projectRootElement))
            {
                var useWinForms = projectRootElement.CreatePropertyElement(DesktopFacts.UseWinFormsPropertyName);
                useWinForms.Value = "true";
                propGroup.AppendChild(useWinForms);
            }

            if (!baselineProject.GlobalProperties.Contains(DesktopFacts.UseWPFPropertyName, StringComparer.OrdinalIgnoreCase)
                && MSBuildHelpers.IsWPF(projectRootElement))
            {
                var useWPF = projectRootElement.CreatePropertyElement(DesktopFacts.UseWPFPropertyName);
                useWPF.Value = "true";
                propGroup.AppendChild(useWPF);
            }

            return projectRootElement;
        }

        public static IProjectRootElement AddCommonPropertiesToTopLevelPropertyGroup(this IProjectRootElement projectRootElement)
        {
            var propGroups = projectRootElement.PropertyGroups;

            // If there is only 1, it's the top-level group.
            // If there are only 2, then the remaining group has unqiue properties in it that may be configuration-specific.
            if (propGroups.Count <= 2)
            {
                return projectRootElement;
            }

            var pairs = propGroups.Zip(propGroups.Skip(1), (pgA, pgB) => (pgA, pgB))
                                  .Where(pair => MSBuildHelpers.ArePropertyGroupElementsIdentical(pair.pgA, pair.pgB));

            var topLevelPropGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);

            foreach (var (a, b) in pairs)
            {
                foreach (var prop in a.Properties)
                {
                    if (prop.Parent is { })
                    {
                        a.RemoveChild(prop);
                    }

                    if (!topLevelPropGroup.Properties.Any(p => ProjectPropertyHelpers.ArePropertiesEqual(p, prop)))
                    {
                        topLevelPropGroup.AppendChild(prop);
                    }
                }

                foreach (var prop in b.Properties)
                {
                    if (prop.Parent is { })
                    {
                        b.RemoveChild(prop);
                    }
                }

                if (a.Parent is { })
                {
                    projectRootElement.RemoveChild(a);
                }

                if (b.Parent is { })
                {
                    projectRootElement.RemoveChild(b);
                }
            }

            return projectRootElement;
        }

        public static IProjectRootElement AddGenerateAssemblyInfo(this IProjectRootElement projectRootElement)
        {
            // Don't create a new prop group; put the desktop properties in the same group as where TFM is located
            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroupWithTFM(projectRootElement);
            var generateAssemblyInfo = projectRootElement.CreatePropertyElement(MSBuildFacts.GenerateAssemblyInfoNodeName);
            generateAssemblyInfo.Value = "false";
            propGroup.AppendChild(generateAssemblyInfo);
            return projectRootElement;
        }

        public static IProjectRootElement ModifyProjectElement(this IProjectRootElement projectRootElement)
        {
            projectRootElement.ToolsVersion = null;
            projectRootElement.DefaultTargets = null;
            return projectRootElement;
        }

        public static IProjectRootElement AddTargetFrameworkProperty(this IProjectRootElement projectRootElement, BaselineProject baselineProject, out string targetFrameworkMoniker)
        {
            static string StripDecimals(string tfm)
            {
                var parts = tfm.Split('.');
                return string.Join("", parts);
            }

            if (baselineProject.GlobalProperties.Contains("TargetFramework", StringComparer.OrdinalIgnoreCase))
            {
                // The original project had a TargetFramework property. No need to add it again.
                targetFrameworkMoniker = baselineProject.GlobalProperties.First(p => p.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase));
                return projectRootElement;
            }

            var propGroup = MSBuildHelpers.GetOrCreateTopLevelPropertyGroup(baselineProject, projectRootElement);

            var targetFrameworkElement = projectRootElement.CreatePropertyElement("TargetFramework");

            if (baselineProject.ProjectStyle == ProjectStyle.WindowsDesktop)
            {
                targetFrameworkElement.Value = Facts.MSBuildFacts.NETCoreDesktopTFM;
            }
            else
            {
                var rawTFM = baselineProject.Project.FirstConfiguredProject.GetProperty("TargetFramework").EvaluatedValue;

                // This is pretty much never gonna happen, but it was cheap to write the code
                targetFrameworkElement.Value = MSBuildHelpers.IsNotNetFramework(rawTFM) ? StripDecimals(rawTFM) : rawTFM;
            }

            propGroup.PrependChild(targetFrameworkElement);

            targetFrameworkMoniker = targetFrameworkElement.Value;
            return projectRootElement;
        }
    }
}
