using System;
using System.IO;
using System.Reflection;
using CommandLine;

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
                    var msbuildPath = HookAssemblyResolveForMSBuild(optionsValue);
                    if (msbuildPath is object)
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

        private static string HookAssemblyResolveForMSBuild(Options options)
        {
            var msbuildPath = GetMSBuildPath(options);
            if (msbuildPath == null)
            {
                Console.WriteLine("Cannot find MSBuild. Please pass in a path to msbuild using -m or run from a developer command prompt.");
                return null;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                var targetAssembly = Path.Combine(msbuildPath, new AssemblyName(eventArgs.Name).Name + ".dll");
                return File.Exists(targetAssembly) ? Assembly.LoadFrom(targetAssembly) : null;
            };

            return msbuildPath;
        }

        private static int Run(Options options)
        {
            try
            {
                var projectLoader = new ProjectLoader();
                projectLoader.LoadProjects(options);

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
                        var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory);
                        converter.GenerateProjectFile(opt.OutputProjectPath ?? opt.ProjectFilePath);
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

        private static string GetMSBuildPath(Options options)
        {
            // If the user specified a msbuild path use that.
            if (!string.IsNullOrEmpty(options.MSBuildPath))
            {
                return options.MSBuildPath;
            }

            // If the user is running from a developer command prompt use the MSBuild of that VS
            var vsinstalldir = Environment.GetEnvironmentVariable("VSINSTALLDIR");
            if (!string.IsNullOrEmpty(vsinstalldir))
            {
                var path = Path.Combine(vsinstalldir, "MSBuild", "16.0", "Bin");
                Console.WriteLine($"Found VS from VSINSTALLDIR (Dev Console): {path}");
                return path;
            }

            // herpty derp
            var pathOnVS = Path.Combine(@"C:\Program Files (x86)\Microsoft Visual Studio", "2019", "Preview", "MSBuild", "Current", "Bin");
            if (!string.IsNullOrEmpty(pathOnVS))
            {
                Console.WriteLine($"Found MSBuild from hardcoded location: {pathOnVS}");
                return pathOnVS;
            }

            //Second chance for mono 
            var systemLibLocation = typeof(object).Assembly.Location;
            var monoMSBuildPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(systemLibLocation), "..", "msbuild", "16.0", "bin"));
            if (Directory.Exists(monoMSBuildPath))
            {
                return Path.GetFullPath(monoMSBuildPath);
            }

            return null;
        }
    }
}
