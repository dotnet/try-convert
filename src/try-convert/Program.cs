using System;
using System.Threading.Tasks;
using System.CommandLine;
using Conversion;
using MSBuild.Abstractions;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;

namespace TryConvert
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder(new Command("try-convert", handler: CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)))))
                .UseParseDirective()
                .UseHelp()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-p", "--project" }, "The path to a project to convert", new Argument<string>()))
                .AddOption(new Option(new [] { "-w", "--workspace" }, "The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.", new Argument<string>()))
                .AddOption(new Option(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that", new Argument<string>()))
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done", new Argument<bool>()))
                .AddOption(new Option(new[] { "--no-backup"}, "Converts projects and does not create a backup of the originals.", new Argument<bool>()))
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static int Run(string project, string workspace, string msbuildPath, bool diffOnly, bool noBackup)
        {
            if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(workspace))
            {
                Console.WriteLine("Cannot specify both a project and a workspace.");
                return -1;
            }

            try
            {
                msbuildPath = MSBuildHelpers.HookAssemblyResolveForMSBuild(msbuildPath);
                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    Console.WriteLine("Could not find an MSBuild.");
                    return -1;
                }

                var currentDirectory = Environment.CurrentDirectory;
                string workspacePath = string.Empty;
                MSBuildWorkspaceType workspaceType;

                if (!string.IsNullOrWhiteSpace(project))
                {
                    workspacePath = Path.GetFullPath(project, Environment.CurrentDirectory);
                    workspaceType = MSBuildWorkspaceType.Project;
                }
                else if (!string.IsNullOrWhiteSpace(workspace))
                {
                    var (isSolution, workspaceFilePath) = MSBuildWorkspaceFinder.FindWorkspace(currentDirectory, workspace);
                    workspaceType = isSolution ? MSBuildWorkspaceType.Solution : MSBuildWorkspaceType.Project;
                    workspacePath = workspaceFilePath;
                }
                else
                {
                    throw new ArgumentException("No valid arguments to fulfill a workspace are given.");
                }

                var workspaceLoader = new MSBuildWorkspaceLoader(workspacePath, workspaceType);
                var msbuildWorkspace = workspaceLoader.LoadWorkspace(workspacePath, noBackup);

                foreach (var item in msbuildWorkspace.WorkspaceItems)
                {
                    if (diffOnly)
                    {
                        var differ = new Differ(item.UnconfiguredProject.FirstConfiguredProject, item.SdkBaselineProject.Project.FirstConfiguredProject);
                        differ.GenerateReport(workspacePath);
                    }
                    else
                    {
                        var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement);
                        converter.Convert(item.ProjectRootElement.FullPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }

            Console.WriteLine("Conversion complete!");
            return 0;
        }
    }
}
