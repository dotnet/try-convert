using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace ProjectSimplifier
{
    internal static class MSBuildUtilities
    {
        /// <summary>
        /// matches $(name) pattern
        /// </summary>
        private static readonly Regex DimensionNameInConditionRegex = new Regex(@"^\$\(([^\$\(\)]*)\)$");

        /// <summary>
        /// Converts configuration dimensional value vector to a msbuild condition
        /// Use the standard format of
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="dimensionalValues">vector of configuration dimensional properties</param>
        /// <returns>msbuild condition representation</returns>
        internal static string DimensionalValuePairsToCondition(ImmutableDictionary<string, string> dimensionalValues)
        {
            if (null == dimensionalValues || 0 == dimensionalValues.Count)
            {
                return string.Empty; // no condition. Returns empty string to match MSBuild.
            }

            string left = string.Empty;
            string right = string.Empty;

            foreach (string key in dimensionalValues.Keys)
            {
                if (!string.IsNullOrEmpty(left))
                {
                    left = left + "|";
                    right = right + "|";
                }

                left += "$(" + key + ")";
                right += dimensionalValues[key];
            }

            string condition = "'" + left + "'=='" + right + "'";
            return condition;
        }

        /// <summary>
        /// Returns a name of a configuration like Debug|AnyCPU
        /// </summary>
        internal static string GetConfigurationName(ImmutableDictionary<string, string> dimensionValues) => dimensionValues.IsEmpty ? "" : dimensionValues.Values.Aggregate((x, y) => $"{x}|{y}");

        /// <summary>
        /// Returns a name of a configuration like Debug|AnyCPU
        /// </summary>
        internal static string GetConfigurationName(string condition)
        {
            if (ConditionToDimensionValues(condition, out var dimensionValues))
            {
                return GetConfigurationName(dimensionValues);
            }

            return "";
        }

        /// <summary>
        /// Tries to parse an MSBuild condition to a dimensional vector
        /// only matches standard pattern:
        /// '$(DimensionName1)|$(DimensionName2)|...|$(DimensionNameN)'=='DimensionValue1|...|DimensionValueN'
        /// </summary>
        /// <param name="condition">msbuild condition string</param>
        /// <param name="dimensionalValues">configuration dimensions vector (output)</param>
        /// <returns>true on success</returns>
        internal static bool ConditionToDimensionValues(string condition, out ImmutableDictionary<string, string> dimensionalValues)
        {
            string left;
            string right;
            dimensionalValues = ImmutableDictionary<string, string>.Empty;

            if (string.IsNullOrEmpty(condition))
            {
                // yes empty condition is recognized as a empty dimension vector
                return true;
            }

            int equalPos = condition.IndexOf("==", StringComparison.OrdinalIgnoreCase);
            if (equalPos <= 0)
            {
                return false;
            }

            left = condition.Substring(0, equalPos).Trim();
            right = condition.Substring(equalPos + 2).Trim();

            // left and right needs to ba a valid quoted strings
            if (!UnquoteString(ref left) || !UnquoteString(ref right))
            {
                return false;
            }

            string[] dimensionNamesInCondition = left.Split(new char[] { '|' });
            string[] dimensionValuesInCondition = right.Split(new char[] { '|' });

            // number of keys need to match number of values
            if (dimensionNamesInCondition.Length == 0 || dimensionNamesInCondition.Length != dimensionValuesInCondition.Length)
            {
                return false;
            }

            Dictionary<string, string> parsedDimensionalValues = new Dictionary<string, string>(dimensionNamesInCondition.Length);

            for (int i = 0; i < dimensionNamesInCondition.Length; i++)
            {
                // matches "$(name)" patern.
                Match match = DimensionNameInConditionRegex.Match(dimensionNamesInCondition[i]);
                if (!match.Success)
                {
                    return false;
                }

                string dimensionName = match.Groups[1].ToString();
                if (string.IsNullOrEmpty(dimensionName))
                {
                    return false;
                }

                parsedDimensionalValues[dimensionName] = dimensionValuesInCondition[i];
            }

            dimensionalValues = parsedDimensionalValues.ToImmutableDictionary();
            return true;
        }

        internal static bool FrameworkHasAValueTuple(string tfm)
        {
            if (tfm is null
                || tfm.ContainsIgnoreCase(Facts.NetstandardPrelude, StringComparison.CurrentCultureIgnoreCase)
                || tfm.ContainsIgnoreCase(Facts.NetcoreappPrelude, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (!tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return tfm.StartsWith(Facts.LowestFrameworkVersionWithSystemValueTuple);
        }

        internal static bool IsPackageReference(ProjectItemElement element) => element.ElementName.Equals(Facts.PackageReferenceItemType, StringComparison.OrdinalIgnoreCase);

        internal static IEnumerable<ProjectItemElement> GetCandidateItemsForRemoval(ProjectItemGroupElement itemGroup) =>
            itemGroup.Items.Where(item => item.ElementName.Equals(Facts.MSBuildReferenceName, StringComparison.OrdinalIgnoreCase)
                                          || Facts.GlobbedItemTypes.Contains(item.ElementName, StringComparer.OrdinalIgnoreCase));

        internal static IEnumerable<ProjectItemElement> GetReferences(ProjectItemGroupElement itemGroup) =>
            itemGroup.Items.Where(item => item.ElementName.Equals(Facts.MSBuildReferenceName, StringComparison.OrdinalIgnoreCase));

        internal static bool IsWPF(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include);
            return Facts.KnownWPFReferences.All(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
        }

        internal static bool IsWinForms(IProjectRootElement projectRoot)
        {
            var references = projectRoot.ItemGroups.SelectMany(GetReferences)?.Select(elem => elem.Include);
            return Facts.KnownWinFormsReferences.All(reference => references.Contains(reference, StringComparer.OrdinalIgnoreCase));
        }

        internal static bool IsNotNetFramework(string tfm) => 
            !tfm.ContainsIgnoreCase(Facts.NetcoreappPrelude, StringComparison.OrdinalIgnoreCase)
            && !tfm.ContainsIgnoreCase(Facts.NetstandardPrelude, StringComparison.OrdinalIgnoreCase);


        internal static bool DesktopReferencesNeedsRemoval(ProjectItemElement item) =>
            Facts.ItemsWithPackagesThatWorkOnNETCore.Contains(item.Include, StringComparer.OrdinalIgnoreCase)
            || Facts.DesktopReferencesThatNeedRemoval.Contains(item.Include, StringComparer.OrdinalIgnoreCase);

        internal static bool IsExplicitValueTupleReferenceNeeded(ProjectItemElement item, string tfm) =>
            item.Include.Equals(Facts.SystemValueTupleName, StringComparison.OrdinalIgnoreCase) && MSBuildUtilities.FrameworkHasAValueTuple(tfm);

        /// <summary>
        /// Checks if the given item is a designer file that is not one of { Settings.Designer.cs, Resources.Designer.cs }.
        /// </summary>
        /// <param name="item">The ProjectItemElement that might be a designer file.</param>
        /// <returns>true if the given ProjectItemElement is a designer file that isn't a settings or resources file.</returns>
        internal static bool IsWinFormsUIDesignerFile(ProjectItemElement item) =>
            item.Include.EndsWith(Facts.DesignerEndString, StringComparison.OrdinalIgnoreCase)
            && !item.Include.EndsWith(Facts.ResourcesDesignerFileName, StringComparison.OrdinalIgnoreCase)
            && !item.Include.EndsWith(Facts.SettingsDesignerFileName, StringComparison.OrdinalIgnoreCase);

        internal static bool IsDefineConstantDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(Facts.DefineConstantsName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Split(';').All(constant => Facts.DefaultDefineConstants.Contains(constant, StringComparer.OrdinalIgnoreCase));

        internal static bool IsDebugTypeDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(Facts.DebugTypeName, StringComparison.OrdinalIgnoreCase)
            && Facts.DefaultDebugTypes.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        internal static bool IsOutputPathDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(Facts.OutputPathName, StringComparison.OrdinalIgnoreCase)
            && Facts.DefaultOutputPaths.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        internal static bool IsPlatformTargetDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(Facts.PlatformTargetName, StringComparison.OrdinalIgnoreCase)
            && Facts.DefaultPlatformTargets.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        internal static bool IsLegacyXamlDesignerItem(ProjectItemElement item) =>
            item.Include.EndsWith(Facts.XamlFileExtension, StringComparison.OrdinalIgnoreCase)
            && item.Metadata.Any(pme => pme.Name.Equals(Facts.SubTypeName, StringComparison.OrdinalIgnoreCase)
                                       && pme.Value.Equals(Facts.DesignerSubType, StringComparison.OrdinalIgnoreCase));

        internal static bool IsDependentUponXamlDesignerItem(ProjectItemElement item) =>
            item.Metadata.Any(pme => pme.Name.Equals(Facts.SubTypeName, StringComparison.OrdinalIgnoreCase) && pme.Value.Equals(Facts.CodeSubType, StringComparison.OrdinalIgnoreCase))
            && item.Metadata.Any(pme => pme.Name.Equals(Facts.DependentUponName, StringComparison.OrdinalIgnoreCase) && pme.Value.EndsWith(Facts.XamlFileExtension, StringComparison.OrdinalIgnoreCase));

        internal static ProjectItemGroupElement GetPackagesConfigItemGroup(IProjectRootElement root) =>
            root.ItemGroups.Single(pige => pige.Items.Any(pe => pe.Include.Equals(Facts.PackagesConfigIncludeName, StringComparison.OrdinalIgnoreCase)));

        internal static ProjectItemElement GetPackagesConfigItem(ProjectItemGroupElement packagesConfigItemGroup) =>
            packagesConfigItemGroup.Items.Single(pe => pe.Include.Equals(Facts.PackagesConfigIncludeName, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Unquote string. It simply removes the starting and ending "'", and checks they are present before.
        /// </summary>
        /// <param name="s">string to unquote </param>
        /// <returns>true if string is successfuly unquoted</returns>
        private static bool UnquoteString(ref string s)
        {
            if (s.Length < 2 || s[0] != '\'' || s[s.Length - 1] != '\'')
            {
                return false;
            }

            s = s.Substring(1, s.Length - 2);
            return true;
        }
    }
}
