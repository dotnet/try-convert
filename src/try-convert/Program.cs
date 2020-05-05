using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

using MSBuild.Abstractions;
using MSBuild.Conversion.Project;
using MSBuild.Conversion.SDK;

namespace MSBuild.Conversion
{
    internal class Program
    {
        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.Name = "try-convert";
            rootCommand.Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)));

            var parser =
                new CommandLineBuilder(rootCommand)
                .UseParseDirective()
                .UseHelp()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .AddOption(new Option(new[] { "-p", "--project" }, "The path to a project to convert") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-w", "--workspace" }, "The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "-tfm", "--target-framework" }, "The name of the framework you would like to upgrade to. Useful if you'd prefer not to change TFMs to .NET Core just yet.") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "--preview" }, "Use preview SDKs as part of conversion") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--no-backup" }, "Converts projects and does not create a backup of the originals.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--keep-current-tfms" }, "Converts projects but does not change the TFM to .NET Core. Useful if you just move to the .NET SDK, but not .NET Core yet.") { Argument = new Argument<bool>(() => false) })
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static int Run(string? project, string? workspace, string? msbuildPath, string? tfm, bool allowPreviews, bool diffOnly, bool noBackup, bool keepCurrentTfms)
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

                if (!keepCurrentTfms)
                {
                    if (string.IsNullOrWhiteSpace(tfm))
                    {
                        tfm = TargetFrameworkHelper.FindHighestInstalledTargetFramework(allowPreviews);
                    }
                    else
                    {
                        tfm = tfm.Trim();
                        if (!TargetFrameworkHelper.IsValidTargetFramework(tfm))
                        {
                            Console.WriteLine($"Invalid framework specified for --target-framework: '{tfm}'");
                            return -1;
                        }
                    }
                }

                var workspacePath = string.Empty;
                MSBuildWorkspaceType workspaceType;

                if (!string.IsNullOrWhiteSpace(project))
                {
                    workspacePath = Path.GetFullPath(project, Environment.CurrentDirectory);
                    workspaceType = MSBuildWorkspaceType.Project;
                }
                else
                {
                    var (isSolution, workspaceFilePath) = MSBuildWorkspaceFinder.FindWorkspace(Environment.CurrentDirectory, workspace);
                    workspaceType = isSolution ? MSBuildWorkspaceType.Solution : MSBuildWorkspaceType.Project;
                    workspacePath = workspaceFilePath;
                }

                var workspaceLoader = new MSBuildWorkspaceLoader(workspacePath, workspaceType);
                // do not create backup if --diff-only specified
                noBackup = noBackup || diffOnly;
                var msbuildWorkspace = workspaceLoader.LoadWorkspace(workspacePath, noBackup);

                foreach (var item in msbuildWorkspace.WorkspaceItems)
                {
                    if (diffOnly)
                    {
                        var differ = new Differ(item.UnconfiguredProject.FirstConfiguredProject, item.SdkBaselineProject.Project.FirstConfiguredProject);
                        differ.GenerateReport(Directory.GetParent(workspacePath).FullName);
                    }
                    else
                    {
                        var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement);
                        converter.Convert(item.ProjectRootElement.FullPath, tfm, keepCurrentTfms);
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
