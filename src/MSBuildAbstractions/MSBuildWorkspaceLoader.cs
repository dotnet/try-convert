using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace MSBuildAbstractions
{
    public class MSBuildWorkspaceLoader
    {
        private readonly string _workspacePath;
        private readonly string _outputPath;
        private readonly MSBuildWorkspaceType _workspaceType;

        public MSBuildWorkspaceLoader(string workspacePath, MSBuildWorkspaceType workspaceType)
        {
            if (string.IsNullOrWhiteSpace(workspacePath))
            {
                throw new ArgumentException($"{workspacePath} cannot be null or empty.");
            }

            if (!File.Exists(workspacePath))
            {
                throw new FileNotFoundException(workspacePath);
            }

            _workspacePath = workspacePath;
            _workspaceType = workspaceType;
            _outputPath = workspacePath; // TODO - this should actually be a  thing though
        }

        public MSBuildWorkspace LoadWorkspace(string path)
        {
            var projectPaths =
                _workspaceType switch
                {
                    MSBuildWorkspaceType.Project => ImmutableArray.Create(path),
                    MSBuildWorkspaceType.Solution => SolutionFile.Parse(_workspacePath).ProjectsInOrder.Select(p => p.AbsolutePath).ToImmutableArray(),
                    _ => throw new InvalidOperationException("couldn't do literally anything")
                };

            return new MSBuildWorkspace(projectPaths);
        }

        public IProjectRootElement GetRootElementFromProjectFile(string projectFilePath = "")
        {
            var path = Path.GetFullPath(projectFilePath);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"The project file '{projectFilePath}' does not exist or is inaccessible.");
            }

            using var collection = new ProjectCollection();

            return new MSBuildProjectRootElement(ProjectRootElement.Open(path, collection, preserveFormatting: true));
        }
    }
}
