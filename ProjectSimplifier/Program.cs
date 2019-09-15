using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using Microsoft.Build.Locator;

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
                        var converter = new Converter(projectLoader.Project, projectLoader.SdkBaselineProject, projectLoader.ProjectRootElement, projectLoader.ProjectRootDirectory);
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
                return Path.Combine(vsinstalldir, "MSBuild", "Current", "Bin");
            }

            // Attempt to set the version of MSBuild.
            var visualStudioInstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            var instance = visualStudioInstances.Length == 1
                // If there is only one instance of MSBuild on this machine, set that as the one to use.
                ? visualStudioInstances[0]
                // Handle selecting the version of MSBuild you want to use.
                : SelectVisualStudioInstance(visualStudioInstances);

            return instance?.MSBuildPath;
        }

        private static VisualStudioInstance SelectVisualStudioInstance(VisualStudioInstance[] visualStudioInstances)
        {
            Console.WriteLine("Multiple installs of MSBuild detected please select one:");
            for (int i = 0; i < visualStudioInstances.Length; i++)
            {
                Console.WriteLine($"Instance {i + 1}");
                Console.WriteLine($"    Name: {visualStudioInstances[i].Name}");
                Console.WriteLine($"    Version: {visualStudioInstances[i].Version}");
                Console.WriteLine($"    MSBuild Path: {visualStudioInstances[i].MSBuildPath}");
            }

            while (true)
            {
                var userResponse = Console.ReadLine();
                if (int.TryParse(userResponse, out int instanceNumber) &&
                    instanceNumber > 0 &&
                    instanceNumber <= visualStudioInstances.Length)
                {
                    return visualStudioInstances[instanceNumber - 1];
                }
                Console.WriteLine("Input not accepted, try again.");
            }
        }
    }
}
