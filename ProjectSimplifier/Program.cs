using System;
using CommandLine;
using Conversion;
using MSBuildAbstractions;

namespace ProjectSimplifier
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<LogOptions, DiffOptions, ConvertOptions>(args);
            switch (options)
            {
                case Parsed<object> command:
                    var optionsValue = command.Value as Options;
                    var msbuildPath = MSBuildUtilities.HookAssemblyResolveForMSBuild(optionsValue.MSBuildPath);
                    if (!string.IsNullOrWhiteSpace(msbuildPath))
                    {
                        return Run(optionsValue);
                    }
                    return -1;

                case NotParsed<object> notParsed:
                    foreach (var error in notParsed.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return 1;
            }
            return 1;
        }

        private static int Run(Options options)
        {
            try
            {
                var projectLoader = new ProjectLoader();
                projectLoader.LoadProjects(options.ProjectFilePath, options.RoslynTargetsPath, options.MSBuildSdksPath, options.TargetProjectProperties);

                switch (options)
                {
                    case LogOptions opt:
                        projectLoader.Project.FirstConfiguredProject.LogProjectProperties(opt.CurrentProjectLogPath);
                        projectLoader.SdkBaselineProject.Project.FirstConfiguredProject.LogProjectProperties(opt.SdkBaseLineProjectLogPath);
                        break;
                    case DiffOptions opt:
                        var differ = new Differ(projectLoader.Project.FirstConfiguredProject, projectLoader.SdkBaselineProject.Project.FirstConfiguredProject);
                        differ.GenerateReport(opt.DiffReportPath);
                        break;
                    case ConvertOptions opt:
                        var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory, options.ProjectFilePath);
                        converter.Convert(opt.OutputProjectPath ?? opt.ProjectFilePath);
                        break;
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
