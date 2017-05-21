using System;
using CommandLine;

namespace MSBuildSdkDiffer
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<LogOptions, DiffOptions, ConvertOptions>(args);
            switch (options)
            {
                case Parsed<object> command:
                    return Run(command.Value as Options);

                case NotParsed<object> notParsed:
                    foreach(var error in notParsed.Errors)
                    {
                        Console.WriteLine(error);
                    }
                    return 1;
            }
            return 1;
        }

        private static int Run(Options options)
        {
            var projectLoader = new ProjectLoader();
            projectLoader.LoadProjects(options);

            switch (options)
            {
                case LogOptions opt:
                    projectLoader.Project.LogProjectProperties(opt.CurrentProjectLogPath);
                    projectLoader.SdkBaselineProject.LogProjectProperties(opt.SdkBaseLineProjectLogPath);
                    break;
                case DiffOptions opt:
                    var differ = new Differ(projectLoader.Project, projectLoader.SdkBaselineProject);
                    differ.GenerateReport(opt.DiffReportPath);
                    break;
                case ConvertOptions opt:
                    var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement);
                    converter.GenerateProjectFile(opt.OutputProjectPath);
                    break;
            }

            return 0;
        }
    }
}