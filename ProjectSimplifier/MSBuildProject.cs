using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace ProjectSimplifier
{
    /// <summary>
    /// Interface used to Mock access to MSBuild's Project apis.
    /// </summary>
    public interface IProject
    {
        ICollection<IProjectProperty> Properties { get; }

        ICollection<IProjectItem> Items { get; }

        IProjectProperty GetProperty(string name);

        string GetPropertyValue(string name);
    }

    public interface IProjectProperty
    {
        string Name { get; }
        string EvaluatedValue { get; }
        string UnevaluatedValue { get; }
        bool IsDefinedInProject { get; }
    }

    public interface IProjectItem
    {
        string ItemType { get; }
        string EvaluatedInclude { get; }
        IEnumerable<IProjectMetadata> DirectMetadata { get; }
    }

    public interface IProjectMetadata : IEquatable<IProjectMetadata>
    {
        string Name { get; }
        string UnevaluatedValue { get; }
        string EvaluatedValue { get; }
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

        public string UnevaluatedValue => _property.UnevaluatedValue;

        public bool IsDefinedInProject => !_property.IsImported && 
                                          !_property.IsEnvironmentProperty && 
                                          !_property.IsGlobalProperty &&
                                          !_property.IsReservedProperty;
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

        public IEnumerable<IProjectMetadata> DirectMetadata => _item.DirectMetadata.Select(md => new MSBuildProjectMetadata(md));
    }

    internal class MSBuildProjectMetadata : IProjectMetadata
    {
        private readonly ProjectMetadata _projectMetadata;

        public MSBuildProjectMetadata(ProjectMetadata projectMetadata)
        {
            _projectMetadata = projectMetadata;
        }
        
        public string Name => _projectMetadata.Name;

        public string UnevaluatedValue => _projectMetadata.UnevaluatedValue;

        public string EvaluatedValue => _projectMetadata.EvaluatedValue;

        public bool Equals(IProjectMetadata other)
        {
            return _projectMetadata.Name.Equals(other.Name) &&
                   _projectMetadata.UnevaluatedValue.Equals(other.UnevaluatedValue) &&
                   _projectMetadata.EvaluatedValue.Equals(other.EvaluatedValue);
        }
    }

    internal class MSBuildProject : IProject
    {
        private readonly Project _project;

        public MSBuildProject(Project project) => _project = project ?? throw new ArgumentNullException(nameof(project));

        public ICollection<IProjectProperty> Properties => _project.Properties.Select(p => new MSBuildProjectProperty(p)).ToArray();

        public ICollection<IProjectItem> Items => _project.Items.Select(i => new MSBuildProjectItem(i)).ToArray();

        public IProjectProperty GetProperty(string name) => _project.GetProperty(name) != null ? new MSBuildProjectProperty(_project.GetProperty(name)) : null;

        public string GetPropertyValue(string name) => _project.GetPropertyValue(name);
    }
}
