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

    [Verb("log", HelpText = "Log properties and items in the project and in a SDK-based baseline")]
    internal class LogOptions : Options
    {
        [Option('c', "currentProjectLogPath",
                HelpText = "Location to log the current project's properties and  items",
                Default = "currentProject.log")]
        public string CurrentProjectLogPath { get; set; }

        [Option('b', "sdkBaseLineProjectLogPath",
        HelpText = "Location to log a sdk baseline project's properties and  items",
        Default = "sdkBaseLineProject.log")]
        public string SdkBaseLineProjectLogPath { get; set; }
    }

    [Verb("diff", HelpText = "Diff a given project against a SDK baseline")]
    internal class DiffOptions : Options
    {
        [Option('d', "diffReportPath",
                HelpText = "Location to output a diff of the current project against a sdk baseline",
                Default = "diffreport.diff")]
        public string DiffReportPath { get; set; }
    }

    [Verb("convert", HelpText = "Convert a given project to be based on the SDK")]
    internal class ConvertOptions : Options
    {
        [Option('o', "outputProjectPath",
                HelpText = "Location to output the converted project",
                Required = true)]
        public string OutputProjectPath { get; set; }
    }
}
