using System;
using System.Threading.Tasks;
using System.CommandLine;
using Conversion;
using MSBuildAbstractions;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;

namespace TryConvert
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var parser = new CommandLineBuilder(new Command("try-format", handler: CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)))))
                .UseParseDirective()
                .UseHelp()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-p", "--project" }, "The path to a project to convert", new Argument<string>()))
                .AddOption(new Option(new[] { "-f", "--folder" }, "The path to a project to convert", new Argument<string>()))
                .AddOption(new Option(new [] { "-w", "--workspace" }, "The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.", new Argument<string>()))
                .AddOption(new Option(new[] { "-o", "--output" }, "The output path to write the converted project to", new Argument<string>()))
                .AddOption(new Option(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that", new Argument<string>()))
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done", new Argument<bool>()))
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static int Run(string project, string folder, string workspace, string output, string msbuildPath, bool diffOnly)
        {
            if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(workspace))
            {
                Console.WriteLine("Cannot specify both a project and a workspace.");
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(project) && (!string.IsNullOrWhiteSpace(folder)))
            {
                Console.WriteLine("Cannot specify both a project and a folder.");
                return -1;
            }

            if (!string.IsNullOrWhiteSpace(folder) && !string.IsNullOrWhiteSpace(workspace))
            {
                Console.WriteLine("Cannot specify both a folder and a workspace.");
                return -1;
            }

            var currentDirectory = string.Empty;

            try
            {
                msbuildPath = MSBuildUtilities.HookAssemblyResolveForMSBuild(msbuildPath);
                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    Console.WriteLine("Could not find an MSBuild.");
                    return -1;
                }

                currentDirectory = Environment.CurrentDirectory;
                string workspacePath = string.Empty;
                MSBuildWorkspaceType workspaceType;

                if (!string.IsNullOrWhiteSpace(project))
                {
                    workspacePath = Path.GetFullPath(project, Environment.CurrentDirectory);
                    workspaceType = MSBuildWorkspaceType.Project;
                }
                else if (!string.IsNullOrWhiteSpace(folder))
                {
                    workspacePath = Path.GetFullPath(folder, Environment.CurrentDirectory);
                    workspaceType = MSBuildWorkspaceType.Folder;
                }
                else if (!string.IsNullOrWhiteSpace(workspace))
                {
                    var (isSolution, workspaceFilePath) = MSBuildWorkspaceFinder.FindWorkspace(currentDirectory, workspace);
                    workspaceType = isSolution ? MSBuildWorkspaceType.Solution : MSBuildWorkspaceType.Project;
                }
                else
                {
                    throw new ArgumentException("No valid arguments to fulfill a workspace are given.");
                }



                //var projectLoader = new MSBuildWorkspaceLoader();
                //projectLoader.LoadWorkspace(workspace);

                //if (diffOnly)
                //{
                //    var differ = new Differ(projectLoader.Project.FirstConfiguredProject, projectLoader.SdkBaselineProject.Project.FirstConfiguredProject);
                //    differ.GenerateReport(output);
                //}
                //else
                //{
                //    var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory, project);
                //    var path = string.IsNullOrWhiteSpace(output) ? workspace : output;
                //    converter.Convert(path);
                //}
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
