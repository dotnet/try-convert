using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    /// <summary>
    /// Interface used to Mock access to MSBuild's Project apis.
    /// </summary>
    public interface IProject
    {
        ICollection<ProjectProperty> Properties { get; }

        ICollection<IProjectItem> Items { get; }

        IProjectProperty GetProperty(string name);

        string GetPropertyValue(string name);
    }

    public interface IProjectProperty
    {
        string Name { get; }
        string EvaluatedValue { get; }
    }

    public interface IProjectItem
    {
        string ItemType { get; }
        string EvaluatedInclude { get; }
    }

    internal class MSBuildProjectProperty : IProjectProperty
    {
        private readonly ProjectProperty _property;

        public MSBuildProjectProperty(ProjectProperty property)
        {
            _property = property;
        }

        public string Name => _property.Name;

        public string EvaluatedValue => _property.EvaluatedValue;
    }

    internal class MSBuildProjectItem : IProjectItem
    {
        private readonly ProjectItem _item;

        public MSBuildProjectItem(ProjectItem item)
        {
            _item = item;
        }

        public string ItemType => _item.ItemType;

        public string EvaluatedInclude => _item.EvaluatedInclude;
    }

    internal class MSBuildProject : IProject
    {
        private readonly Project _project;

        public MSBuildProject(Project project) => _project = project ?? throw new ArgumentNullException(nameof(project));

        public ICollection<ProjectProperty> Properties => _project.Properties;

        public ICollection<IProjectItem> Items => _project.Items.Select(i => new MSBuildProjectItem(i)).ToArray();

        public IProjectProperty GetProperty(string name) => new MSBuildProjectProperty(_project.GetProperty(name));

        public string GetPropertyValue(string name) => _project.GetPropertyValue(name);
    }
}
