using CommandLine;

namespace MSBuildSdkDiffer
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
    }
}
