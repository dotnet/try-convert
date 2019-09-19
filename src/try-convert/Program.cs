using System;
using System.Threading.Tasks;
using System.CommandLine;
using Conversion;
using MSBuildAbstractions;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;

namespace ProjectSimplifier
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
                .AddOption(new Option(new [] { "-p", "--project" }, "The path to a project to convert", new Argument<string>()))
                .AddOption(new Option(new[] { "-o", "--output" }, "The output path to write the converted project to", new Argument<string>()))
                .AddOption(new Option(new[] { "-m", "--msbuild-path" }, "The path to an MSBuild.exe, if you prefer to use that", new Argument<string>()))
                .AddOption(new Option(new[] { "--diff-only" }, "Produces a diff of the project to convert; no conversion is done", new Argument<bool>()))
                .Build();


            return await parser.InvokeAsync(args).ConfigureAwait(false);
        }

        public static int Run(string project, string output, string msbuildPath, bool diffOnly)
        {
            msbuildPath = MSBuildHelpers.HookAssemblyResolveForMSBuild(msbuildPath);
            if (string.IsNullOrWhiteSpace(msbuildPath))
            {
                return -1;
            }

            try
            {
                var projectLoader = new ProjectLoader();
                projectLoader.LoadProjects(project);

                if (diffOnly)
                {
                    var differ = new Differ(projectLoader.Project.FirstConfiguredProject, projectLoader.SdkBaselineProject.Project.FirstConfiguredProject);
                    differ.GenerateReport(output);
                }
                else
                {
                    var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory, project);
                    var path = string.IsNullOrWhiteSpace(output) ? project : output;
                    converter.Convert(path);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }

            return 0;
        }
    }
}
