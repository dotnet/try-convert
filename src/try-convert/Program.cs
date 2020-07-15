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
            var rootCommand = new RootCommand
            {
                Name = "try-convert",
                Handler = CommandHandler.Create(typeof(Program).GetMethod(nameof(Run)))
            };

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
                .AddOption(new Option(new[] { "-tfm", "--target-framework" }, "The name of the framework you would like to upgrade to. If unspecified, the default TFM for apps chosen will be the highest available one found on your machine, and the default TFM for libraries will be .NET Standard 2.0.") { Argument = new Argument<string?>(() => null) })
                .AddOption(new Option(new[] { "--preview" }, "Use preview SDKs as part of conversion") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--no-backup" }, "Converts projects and does not create a backup of the originals.") { Argument = new Argument<bool>(() => false) })
                .AddOption(new Option(new[] { "--keep-current-tfms" }, "Converts project files but does not change any TFMs. If unspecified, TFMs may change.") { Argument = new Argument<bool>(() => false) })
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

            if (!string.IsNullOrWhiteSpace(tfm) && keepCurrentTfms)
            {
                Console.WriteLine($"Both '{nameof(tfm)}' and '{nameof(keepCurrentTfms)}' cannot be specified. Please pick one.");
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

                if (!string.IsNullOrWhiteSpace(tfm))
                {
                    tfm = tfm.Trim();
                    if (!TargetFrameworkHelper.IsValidTargetFramework(tfm))
                    {
                        Console.WriteLine($"Invalid framework specified for --target-framework: '{tfm}'");
                        return -1;
                    }
                }

                var workspacePath = string.Empty;
                MSBuildConversionWorkspaceType workspaceType;

                if (!string.IsNullOrWhiteSpace(project))
                {
                    workspacePath = Path.GetFullPath(project, Environment.CurrentDirectory);
                    workspaceType = MSBuildConversionWorkspaceType.Project;
                }
                else
                {
                    var (isSolution, workspaceFilePath) = MSBuildConversionWorkspaceFinder.FindWorkspace(Environment.CurrentDirectory, workspace);
                    workspaceType = isSolution ? MSBuildConversionWorkspaceType.Solution : MSBuildConversionWorkspaceType.Project;
                    workspacePath = workspaceFilePath;
                }

                var workspaceLoader = new MSBuildConversionWorkspaceLoader(workspacePath, workspaceType);
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
                        converter.Convert(item.ProjectRootElement.FullPath, tfm, keepCurrentTfms, allowPreviews);
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
