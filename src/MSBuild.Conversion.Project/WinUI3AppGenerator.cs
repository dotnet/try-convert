using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Construction;
using MSBuild.Abstractions;
using MSBuild.Conversion.Facts;

namespace MSBuild.Conversion.Project
{
    internal class WinUI3AppGenerator
    {
        private static readonly Regex MatchCsProjFileRegex = new Regex(@"([^/\\]+$)");
        private static readonly Regex MatchCsProjExtensionRegex = new Regex(@"([^.]+$)");

        internal static void GenerateWapproj(IProjectRootElement projectRootElement, string outputPath)
        {
            //first get the new path to a wapproj file
            string csProjPath = outputPath;
            string csProjFile = MatchCsProjFileRegex.Match(csProjPath).Value;
            string wapProjFile = MatchCsProjExtensionRegex.Replace(csProjPath, "wapproj");

            //Need to see if one already exists, if it does, (why would it?) overwrite with .old
            if (File.Exists(wapProjFile))
            {
                File.Copy(wapProjFile, wapProjFile + ".old", overwrite: true);
            }

            // go through project root element and remove all the properties that belong in the wapproj
            var wapProps = new List<ProjectPropertyElement>();
            var wapItems = new List<ProjectItemElement>();
            foreach (var propGroup in projectRootElement.PropertyGroups)
            {
                foreach (var prop in propGroup.Properties)
                {
                    if (prop.Name.StartsWith("Appx", StringComparison.OrdinalIgnoreCase)
                        || WinUIFacts.WapprojProperties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        propGroup.RemoveChild(prop);
                        wapProps.Add(prop);
                    }
                }
                if (propGroup.Properties.Count == 0)
                {
                    projectRootElement.RemoveChild(propGroup);
                }
            }
            foreach (var itemGroup in projectRootElement.ItemGroups)
            {
                foreach (var item in itemGroup.Items)
                {
                    if (WinUIFacts.WapprojItems.Contains(item.ElementName))
                    {
                        itemGroup.RemoveChild(item);
                        wapItems.Add(item);
                    }
                }
                if (itemGroup.Items.Count == 0)
                {
                    projectRootElement.RemoveChild(itemGroup);
                }
            }

            // fill in template and create new file
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.CloseOutput = true;
            settings.OmitXmlDeclaration = false;

            XmlWriter writer = XmlWriter.Create(wapProjFile, settings);

            writer.WriteStartElement("Project");

            writer.WriteStartElement("PropertyGroup");
            foreach (var p in wapProps)
            {
                if (p.Value.Trim().Equals(string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    // if empty write singelton
                    writer.WriteStartElement(p.Name);
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteElementString(p.Name, p.Value);
                }
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ItemGroup");
            foreach (var i in wapItems)
            {
                writer.WriteStartElement(i.ElementName);
                writer.WriteAttributeString("Include", i.Include);
                foreach (var m in i.Metadata)
                {
                    writer.WriteElementString(m.Name, m.Value);
                }
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteEndElement();
            writer.Flush();
            writer.Close();

        }
    }
}
