using System.Collections.Generic;
using Microsoft.Build.Construction;

namespace ProjectSimplifier
{
    public interface IProjectRootElement
    {
        string ToolsVersion { get; set; }
        string Sdk { get; set; }
        ICollection<ProjectImportElement> Imports { get; }
        ICollection<ProjectImportGroupElement> ImportGroups { get; }
        ICollection<ProjectPropertyGroupElement> PropertyGroups { get; }
        ICollection<ProjectItemGroupElement> ItemGroups { get; }

        void Save(string path);
        void RemoveChild(ProjectElement child);
        ProjectPropertyElement CreatePropertyElement(string propertyName);
        ProjectPropertyGroupElement AddPropertyGroup();
        void Reload(bool throwIfUnsavedChanges = true, bool? preserveFormatting = null);
    }


    internal class MSBuildProjectRootElement : IProjectRootElement
    {
        private readonly ProjectRootElement _rootElement;

        public MSBuildProjectRootElement(ProjectRootElement rootElement)
        {
            _rootElement = rootElement;
        }

        public string ToolsVersion { get => _rootElement.ToolsVersion; set => _rootElement.ToolsVersion = value; }
        public string Sdk { get => _rootElement.Sdk; set => _rootElement.Sdk = value; }
        public ICollection<ProjectImportElement> Imports => _rootElement.Imports;
        public ICollection<ProjectImportGroupElement> ImportGroups => _rootElement.ImportGroups;
        public ICollection<ProjectPropertyGroupElement> PropertyGroups => _rootElement.PropertyGroups;
        public ICollection<ProjectItemGroupElement> ItemGroups => _rootElement.ItemGroups;


        public ProjectPropertyGroupElement AddPropertyGroup() => _rootElement.AddPropertyGroup();
        public ProjectPropertyElement CreatePropertyElement(string name) => _rootElement.CreatePropertyElement(name);

        public void Reload(bool throwIfUnsavedChanges = true, bool? preserveFormatting = null) => _rootElement.Reload(throwIfUnsavedChanges, preserveFormatting);
        public void RemoveChild(ProjectElement child) => _rootElement.RemoveChild(child);
        public void Save(string path) => _rootElement.Save(path);
    }
}
