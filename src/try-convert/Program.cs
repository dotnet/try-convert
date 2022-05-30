using System;
using System.CommandLine;
using System.CommandLine.Builder;
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
        private static Task<int> ErrorResult => Task.FromResult(-1);
        private static Task<int> SuccessResult => Task.FromResult(0);

        private static Option ProjectOption => new Option<string?>(new[] { "-p", "--project" }, "The path to a project to convert");
        private static Option WorkspaceOption => new Option<string?>(new[] { "-w", "--workspace" }, "The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.");
        private static Option MSBuildPathOption => new Option<string?>(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that");
        private static Option TargetFrameworkOption => new Option<string?>(new[] { "-tfm", "--target-framework" }, "The name of the framework you would like to upgrade to. If unspecified, the default TFM for apps chosen will be the highest available one found on your machine, and the default TFM for libraries will be .NET Standard 2.0.");
        private static Option ForceWebConversionOption => new Option<bool>(new[] { "--force-web-conversion" }, "Attempt to convert MVC and WebAPI projects even though significant manual work is necessary after migrating such projects.");
        private static Option PreviewOption => new Option<bool>(new[] { "--preview" }, "Use preview SDKs as part of conversion");
        private static Option DiffOnlyOption => new Option<bool>(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done");
        private static Option NoBackupOption => new Option<bool>(new[] { "--no-backup" }, "Converts projects, does not create a backup of the originals and removes packages.config file.");
        private static Option KeepCurrentTfmsOption => new Option<bool>(new[] { "--keep-current-tfms" }, "Converts project files but does not change any TFMs. If unspecified, TFMs may change.");
        private static Option MauiConverionOption => new Option<bool>(new[] { "--maui-conversion" }, "Attempt to convert Xamarin.Forms Projects to .NET MAUI projects. There may be additional manual work necessary after migrating such projects.");
        private static Option ForceRemoveCustomImportsOption => new Option<bool>(new[] { "--force-remove-custom-imports" }, "Force remove custom imports from the project file if set to true.");
        private static Option UpdateOption => new Option<bool>(new[] { "-u", "--update" }, "Updates the try-convert tool to the latest available version");

        private static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                ProjectOption,
                WorkspaceOption,
                MSBuildPathOption,
                TargetFrameworkOption,
                ForceWebConversionOption,
                PreviewOption,
                DiffOnlyOption,
                NoBackupOption,
                KeepCurrentTfmsOption,
                MauiConverionOption,
                ForceRemoveCustomImportsOption,
                UpdateOption
            };

            Func<string?, string?, string?, string?, bool, bool, bool, bool, bool, bool, bool, bool, Task<int>> handler = Run;

            rootCommand.SetHandler(
                handler,
                ProjectOption,
                WorkspaceOption,
                MSBuildPathOption,
                TargetFrameworkOption,
                ForceWebConversionOption,
                PreviewOption,
                DiffOnlyOption,
                NoBackupOption,
                KeepCurrentTfmsOption,
                MauiConverionOption,
                ForceRemoveCustomImportsOption,
                UpdateOption);

            var parser =
                new CommandLineBuilder(rootCommand)
                .UseParseDirective()
                .UseHelp()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .Build();

            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static Task<int> Run(string? project, string? workspace, string? msbuildPath, string? tfm, bool forceWebConversion, bool preview, bool diffOnly, bool noBackup, bool keepCurrentTfms, bool update, bool mauiConversion, bool forceRemoveCustomImports)
        {
            if (update)
            {
                UpdateTryConvert.Update();
                return SuccessResult;
            }

            if (!string.IsNullOrWhiteSpace(project) && !string.IsNullOrWhiteSpace(workspace))
            {
                Console.WriteLine("Cannot specify both a project and a workspace.");
                return ErrorResult;
            }

            if (!string.IsNullOrWhiteSpace(tfm) && keepCurrentTfms)
            {
                Console.WriteLine($"Both '{nameof(tfm)}' and '{nameof(keepCurrentTfms)}' cannot be specified. Please pick one.");
                return ErrorResult;
            }

            try
            {
                msbuildPath = MSBuildHelpers.HookAssemblyResolveForMSBuild(msbuildPath);

                //For Xamarin Projects, set MSBuild path to VSInstallation Dir via Environment Variable
                if (mauiConversion)
                {
                    // For Xamarin support we need to set the MSBuild Extensions path to VS install.
                    var vsinstalldir = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSINSTALLDIR"))
                        ? Environment.GetEnvironmentVariable("VSINSTALLDIR")
                        : new VisualStudioLocator().GetLatestVisualStudioPath();
                    if (!string.IsNullOrEmpty(vsinstalldir))
                    {
                        Environment.SetEnvironmentVariable("MSBuildExtensionsPath", Path.Combine(vsinstalldir, "MSBuild"));
                    }
                    else
                    {
                        Console.WriteLine("Error locating VS Install Directory. Try setting Environment Variable VSINSTALLDIR.");
                        return ErrorResult;
                    }
                }

                if (string.IsNullOrWhiteSpace(msbuildPath))
                {
                    Console.WriteLine("Could not find an MSBuild.");
                    return ErrorResult;
                }

                if (!string.IsNullOrWhiteSpace(tfm))
                {
                    tfm = tfm.Trim();
                    if (!TargetFrameworkHelper.IsValidTargetFramework(tfm))
                    {
                        Console.WriteLine($"Invalid framework specified for --target-framework: '{tfm}'");
                        return ErrorResult;
                    }
                }
                else
                {
                    tfm = TargetFrameworkHelper.FindHighestInstalledTargetFramework(preview, msbuildPath);
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
                var msbuildWorkspace = workspaceLoader.LoadWorkspace(workspacePath, noBackup, tfm, keepCurrentTfms, forceWebConversion);

                if (msbuildWorkspace.WorkspaceItems.Length is 0)
                {
                    Console.WriteLine("No projects converted.");
                    return SuccessResult;
                }

                foreach (var item in msbuildWorkspace.WorkspaceItems)
                {
                    if (diffOnly)
                    {
                        var differ = new Differ(item.UnconfiguredProject.FirstConfiguredProject, item.SdkBaselineProject.Project.FirstConfiguredProject);
                        var parent = Directory.GetParent(workspacePath);
                        if (parent is null)
                        {
                            differ.GenerateReport(workspacePath);
                        }
                        else
                        {
                            differ.GenerateReport(parent.FullName);
                        }
                    }
                    else
                    {
                        var converter = new Converter(item.UnconfiguredProject, item.SdkBaselineProject, item.ProjectRootElement, noBackup, forceRemoveCustomImports);
                        converter.Convert(item.ProjectRootElement.FullPath);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return ErrorResult;
            }

            Console.WriteLine("Conversion complete!");
            return SuccessResult;
        }
    }
}
