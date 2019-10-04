using MSBuild.Conversion.Facts;
using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSBuild.Abstractions
{
    /// <summary>
    /// Helper functions for working with ProjectPropertyElements
    /// </summary>
    public static class ProjectPropertyHelpers
    {
        /// <summary>
        /// Checks if the given property is the 'Name' property, and if its value is the same as the project file name.
        /// </summary>
        public static bool IsNameDefault(ProjectPropertyElement prop, string projectName) =>
            // TODO use: prop.ContainingProject ?
            prop.ElementName.Equals(MSBuildFacts.NameNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(projectName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'DefineConstants', and if the values defined are the defaults brought in by a template.
        /// </summary>
        public static bool IsDefineConstantDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DefineConstantsName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Split(';').All(constant => MSBuildFacts.DefaultDefineConstants.Contains(constant, StringComparer.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if the given property is 'DebugType', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsDebugTypeDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DebugTypeName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultDebugTypes.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'OutputPath', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsOutputPathDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputPathName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultOutputPaths.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'PlatformTarget', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsPlatformTargetDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.PlatformTargetName, StringComparison.OrdinalIgnoreCase)
            && MSBuildFacts.DefaultPlatformTargets.Contains(prop.Value, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if the given property is 'DocumentationFile', and if the value defined is a default brought in by a template.
        /// </summary>
        public static bool IsDocumentationFileDefault(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.DocumentationFileNodeName, StringComparison.OrdinalIgnoreCase)
            && prop.Value.Equals(MSBuildFacts.DefaultDocumentationFileLocation, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if the name and value of two properties are identical.
        /// </summary>
        public static bool ArePropertiesEqual(ProjectPropertyElement a, ProjectPropertyElement b) =>
            a.ElementName.Equals(b.ElementName, StringComparison.OrdinalIgnoreCase)
            && a.Value.Equals(b.Value, StringComparison.OrdinalIgnoreCase)
            && a.Condition.Equals(b.Condition, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Checks if a property is '<ProjectTypeGuids>'
        /// </summary>
        public static bool IsProjectTypeGuidsNode(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.ProjectTypeGuidsNodeName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Determines if a property lists the default project type GUIds for legacy web projects.
        /// </summary>
        public static bool IsLegacyWebProjectTypeGuidsProperty(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').Any(guidString => MSBuildFacts.LegacyWebProjectTypeGuids.Contains(Guid.Parse(guidString)));

        /// <summary>
        /// Checks if a given OutputType node is wither a library, exe, or WinExe.
        /// </summary>
        public static bool IsSupportedOutputType(ProjectPropertyElement prop) =>
            prop.ElementName.Equals(MSBuildFacts.OutputTypeNodeName, StringComparison.OrdinalIgnoreCase)
            && (prop.Value.Equals(MSBuildFacts.LibraryOutputType, StringComparison.OrdinalIgnoreCase)
                || prop.Value.Equals(MSBuildFacts.ExeOutputType, StringComparison.OrdinalIgnoreCase)
                || prop.Value.Equals(MSBuildFacts.WinExeOutputType, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Checks if a given property defines project type guids for C#, VB.NET, or F#.
        /// </summary>
        public static bool AllProjectTypeGuidsAreLanguageProjectTypeGuids(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').All(guidString => MSBuildFacts.LanguageProjectTypeGuids.Contains(Guid.Parse(guidString)));

        public static bool AllProjectTypeGuidsAreDesktopProjectTypeGuids(ProjectPropertyElement prop) =>
            IsProjectTypeGuidsNode(prop) && prop.Value.Split(';').All(guidString => DesktopFacts.KnownSupportedDesktopProjectTypeGuids.Contains(Guid.Parse(guidString)));
    }
}
