using Facts;

using Microsoft.Build.Construction;

using MSBuildAbstractions;

using PackageConversion;

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Conversion
{
    public class Converter
    {
        private readonly UnconfiguredProject _project;
        private readonly BaselineProject _sdkBaselineProject;
        private IProjectRootElement _projectRootElement;
        private readonly ImmutableDictionary<string, Differ> _differs;

        public Converter(UnconfiguredProject project, BaselineProject sdkBaselineProject, IProjectRootElement projectRootElement)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _sdkBaselineProject = sdkBaselineProject;
            _projectRootElement = projectRootElement ?? throw new ArgumentNullException(nameof(projectRootElement));
            _differs = GetDiffers();
        }

        public void Convert(string outputPath)
        {
            ConvertProjectFile();
            var projectXml = _projectRootElement.Xml;

            // remove all use of xmlns attributes
            projectXml.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var element in projectXml.Descendants())
            {
                element.Name = element.Name.LocalName;
            }

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
            using var writer = XmlWriter.Create(outputPath, writerSettings);
            projectXml.Save(writer);
        }

        internal IProjectRootElement ConvertProjectFile()
        {
            return _projectRootElement
                .ChangeImports(_sdkBaselineProject)
                .RemoveDefaultedProperties(_sdkBaselineProject, _differs)
                .RemoveUnnecessaryPropertiesNotInSDKByDefault()
                .AddTargetFrameworkProperty(_sdkBaselineProject, out var tfm)
                .AddGenerateAssemblyInfo()
                .AddDesktopProperties(_sdkBaselineProject)
                .AddCommonPropertiesToTopLevelPropertyGroup()
                .AddConvertedPackages(tfm)
                .RemoveOrUpdateItems(_differs, _sdkBaselineProject, tfm)
                .AddItemRemovesForIntroducedItems(_differs)
                .ModifyProjectElement();
        }

        internal ImmutableDictionary<string, Differ> GetDiffers() =>
            _project.ConfiguredProjects.Select(p => (p.Key, new Differ(p.Value, _sdkBaselineProject.Project.ConfiguredProjects[p.Key]))).ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Item2);
    }
}
