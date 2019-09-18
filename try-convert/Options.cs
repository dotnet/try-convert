using System.Collections.Generic;
using CommandLine;

namespace ProjectSimplifier
{
    internal class Options
    {
        [Value(0, HelpText = "Full path to the project file", Required = true)]
        public string ProjectFilePath { get; set; }

        [Option('r', "roslyntargetspath",
                HelpText = "Path to the roslyn targets")]
        public string RoslynTargetsPath { get; set; }

        [Option('s', "msbuildsdkdspath",
                HelpText = "Path to the MSBuild SDKs")]
        public string MSBuildSdksPath { get; set; }

        [Option('m', "msbuildpath",
        HelpText = "Path to the MSBuild.exe")]
        public string MSBuildPath { get; set; }

        [Option('p', "properties",
        HelpText = "Properties to set in the target project before converting")]
        public IEnumerable<string> TargetProjectProperties { get; set; }

        [Option('o', "outputpath", HelpText = "Output path to write a converted project to")]
        public string OutputPath { get; set; }
    }
}
