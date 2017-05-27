using System.Collections.Immutable;
using System.Linq;
using Microsoft.Build.Evaluation;

namespace MSBuildSdkDiffer
{
    class UnconfiguredProject
    {
        public ImmutableDictionary<string, IProject> ConfiguredProjects { get; private set; }

        public IProject FirstConfiguredProject => ConfiguredProjects.First().Value;

        public string ProjectFilePath { get; }
        public ImmutableDictionary<string, ImmutableDictionary<string, string>> Configurations { get; }

        public UnconfiguredProject(string projectFilePath, ImmutableDictionary<string, ImmutableDictionary<string, string>> configurations)
        {
            ProjectFilePath = projectFilePath;
            Configurations = configurations;
        }

        internal void LoadProjects(ProjectCollection collection, ImmutableDictionary<string, string> globalProperties)
        {
            var projectBuilder = ImmutableDictionary.CreateBuilder<string, IProject>();
            foreach (var config in Configurations)
            {
                var globalPropertiesWithDimensions = globalProperties.AddRange(config.Value);
                var project = new MSBuildProject(collection.LoadProject(ProjectFilePath, globalPropertiesWithDimensions, null));
                projectBuilder.Add(config.Key, project);

            }

            ConfiguredProjects = projectBuilder.ToImmutable();
        }
    }
}
