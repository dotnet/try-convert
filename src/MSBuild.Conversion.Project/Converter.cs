using System;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using MSBuild.Abstractions;
using MSBuild.Conversion.Facts;
using MSBuild.Conversion.SDK;

namespace MSBuild.Conversion.Project
{
    public class Converter
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
            _differs = GetDiffers();
        }

        public void Convert(string outputPath, string? specifiedTFM, bool keepCurrentTfm, bool usePreviewSDK)
        {
            ConvertProjectFile(specifiedTFM, keepCurrentTfm, usePreviewSDK);
            CleanUpProjectFile(outputPath, true);
        }

        public void ConvertWinUI3(string outputPath, string? specifiedTFM, bool keepCurrentTfm, bool usePreviewSDK)
        {
            var tfm = GetBestTFM(_sdkBaselineProject, keepCurrentTfm, specifiedTFM, usePreviewSDK);
            _projectRootElement.ConvertAndAddPackages(_sdkBaselineProject.ProjectStyle, tfm)
                .RemoveUWPLines(_sdkBaselineProject, tfm)
                .ConvertWinUIItems(_differs, _sdkBaselineProject, tfm);
            CleanUpProjectFile(outputPath, false);
        }

        internal IProjectRootElement? ConvertProjectFile(string? specifiedTFM, bool keepCurrentTfm, bool usePreviewSDK)
        {
            var tfm = GetBestTFM(_sdkBaselineProject, keepCurrentTfm, specifiedTFM, usePreviewSDK); // Este: done

            return _projectRootElement
                // Let's convert packages first, since that's what you should do manually anyways
                .ConvertAndAddPackages(_sdkBaselineProject.ProjectStyle, tfm) //Este: nothing to do, converts old packages.config style

                // Now we can convert the project over
                .ChangeImportsAndAddSdkAttribute(_sdkBaselineProject) // este: Remove old imports and use sdk style
                .RemoveDefaultedProperties(_sdkBaselineProject, _differs) // este: may need to revisit?
                .RemoveUnnecessaryPropertiesNotInSDKByDefault(_sdkBaselineProject.ProjectStyle) // here
                .AddTargetFrameworkProperty(_sdkBaselineProject, tfm)
                .AddGenerateAssemblyInfoAsFalse()
                .AddDesktopProperties(_sdkBaselineProject)
                .AddCommonPropertiesToTopLevelPropertyGroup()
                .RemoveOrUpdateItems(_differs, _sdkBaselineProject, tfm)
                .AddItemRemovesForIntroducedItems(_differs)
                .RemoveUnnecessaryTargetsIfTheyExist()
                .ModifyProjectElement();
        }
        internal static string GetBestTFM(BaselineProject baselineProject, bool keepCurrentTfm, string? specifiedTFM, bool usePreviewSDK)
        {
            if (string.IsNullOrWhiteSpace(specifiedTFM))
            {
                // Let's figure this out, friends
                var tfmForApps = TargetFrameworkHelper.FindHighestInstalledTargetFramework(usePreviewSDK);

                if (keepCurrentTfm)
                {
                    specifiedTFM = baselineProject.GetTfm();
                }
                else if (baselineProject.ProjectStyle == ProjectStyle.WindowsDesktop || baselineProject.ProjectStyle == ProjectStyle.MSTest)
                {
                    specifiedTFM = tfmForApps;
                }
                else if (baselineProject.OutputType == ProjectOutputType.Library)
                {
                    specifiedTFM = MSBuildFacts.Netstandard20;
                }
                else if (baselineProject.OutputType == ProjectOutputType.Exe)
                {
                    specifiedTFM = tfmForApps;
                }
                /* Do Not change the tfm for now, leave as default
                else if (baselineProject.ProjectStyle == ProjectStyle.WinUI) // Este: all winUI proj use net5.0 now
                {
                    specifiedTFM = WinUIFacts.NetCore5;
                }
                */
                else
                {
                    // Default is to just use what exists in the project
                    specifiedTFM = baselineProject.GetTfm();
                }
                // if UWP use net5.0
                // if Desktop use net5.0 
            }
            return specifiedTFM;
        }
        

        internal ImmutableDictionary<string, Differ> GetDiffers() =>
            _project.ConfiguredProjects.Select(p => (p.Key, new Differ(p.Value, _sdkBaselineProject.Project.ConfiguredProjects[p.Key]))).ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Item2);

        private void CleanUpProjectFile(string outputPath, bool removeXMLHeader)
        {
            var projectXml = _projectRootElement.Xml;

            // remove all use of xmlns attributes
            projectXml.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var element in projectXml.Descendants())
            {
                element.Name = element.Name.LocalName;
            }

            // remove all use of ProductVersion
            // this is a property that is used by VS to detect which version of
            // Visual Studio opened this project last and is no longer needed
            projectXml.Descendants().Elements().Where(x => x.Name == "ProductVersion").Remove();
            projectXml.Descendants().Elements().Where(x => x.Name == "FileUpgradeFlags").Remove();
            projectXml.Descendants().Elements().Where(x => x.Name == "UpgradeBackupLocation").Remove();

            // Remove properties that do nothing if they are not set
            projectXml.Descendants().Elements().Where(x => x.Name == "ApplicationIcon" && string.IsNullOrEmpty(x.Value)).Remove();
            projectXml.Descendants().Elements().Where(x => x.Name == "PreBuildEvent" && string.IsNullOrEmpty(x.Value)).Remove();
            projectXml.Descendants().Elements().Where(x => x.Name == "PostBuildEvent" && string.IsNullOrEmpty(x.Value)).Remove();

            // Do not keep comments as the entire file is changing
            var readerSettings = new XmlReaderSettings()
            {
                IgnoreComments = true
            };
            projectXml = XDocument.Load(XmlReader.Create(projectXml.CreateReader(), readerSettings));

            // do not write out xml declaration header
            var writerSettings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
            };
            if (!removeXMLHeader)
            {
                writerSettings.OmitXmlDeclaration = false;
            }
            using var writer = XmlWriter.Create(outputPath, writerSettings);
            projectXml.Save(writer);
        }
    }
}
