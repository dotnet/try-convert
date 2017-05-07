using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    /// <summary>
    /// Interface used to Mock access to MSBuild's Project apis.
    /// </summary>
    public interface IProject
    {
        ICollection<ProjectProperty> Properties { get; }

        ICollection<ProjectItem> Items { get; }

        ProjectProperty GetProperty(string name);

        string GetPropertyValue(string name);
    }

    internal class MSBuildProject : IProject
    {
        private readonly Project _project;

        public MSBuildProject(Project project) => _project = project ?? throw new ArgumentNullException(nameof(project));

        public ICollection<ProjectProperty> Properties => _project.Properties;

        public ICollection<ProjectItem> Items => _project.Items;

        public ProjectProperty GetProperty(string name) => _project.GetProperty(name);

        public string GetPropertyValue(string name) => _project.GetPropertyValue(name);
    }
}
