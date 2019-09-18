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
            var options = Parser.Default.ParseArguments<Options>(args);
            switch (options)
            {
                case Parsed<Options> command:
                    var optionsValue = command.Value;
                    var msbuildPath = MSBuildUtilities.HookAssemblyResolveForMSBuild(optionsValue.MSBuildPath);
                    if (!string.IsNullOrWhiteSpace(msbuildPath))
                    {
                        return Run(optionsValue);
                    }
                    return -1;

                case NotParsed<Options> notParsed:
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
                var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory, options.ProjectFilePath);
                converter.Convert(options.OutputPath ?? options.ProjectFilePath);
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
