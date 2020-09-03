using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class UWPStructTest : TestHelper.CodeFixVerifier
    {
        // This contains code to analyze where no diagnostic should be reported

        // 1. No issues
        private const string noUWP1 = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
            }";

        // Denotes that method is a data test
        [DataTestMethod]
        [DataRow(""), DataRow(noUWP1)]
        // Test Method for valid code with no triggered diagnostics
        public void ProjectionsNoDiagnostics(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        [DataRow("CornerRadius", "FromUniformRadius", "12"),
            DataRow("CornerRadius", "FromRadii", "12, 12, 1, 1"),
            DataRow("Duration", "FromTimeSpan", "Timespan.Zero"),
            DataRow("GridLength", "FromValueAndType", "10, GridUnitType.Pixel"),
            DataRow("GridLength", "FromPixels", "10"),
            DataRow("Thickness", "FromLengths", "12, 11, 10, 9"),
            DataRow("Thickness", "FromUniformLength", "12"),
            DataRow("GeneratorPosition", "FromIndexAndOffset", "10, 10", "Controls.Primitives."),
            DataRow("Matrix", "FromElements", "1, 2, 3, 4 ,5 , 6", "Media."),
            DataRow("KeyTime", "FromTimeSpan", "Timespan.Zero", "Media.Animation."),
            DataRow("RepeatBehavior", "FromCount", "12", "Media.Animation."),
            DataRow("RepeatBehavior", "FromDuration", "Timespan.Zero", "Media.Animation.")]
        public void TestReplaceStruct(string structType, string helperName, string arg, string additionalNamespace = "")
        {
            string test = $@"
                using System;  
                using Windows.UI.Xaml;
                using Windows.UI.Xaml.Controls.Primitives;
                using Windows.UI.Xaml.Media;
                using Windows.UI.Xaml.Media.Animation;
                namespace Test2 {{
                private class Test2
                {{
                    public void test1()
                    {{
                        var repeatBehavior1 = new {structType}({arg});
                    }}
                }}}}";
            string fixTest = $@"
                using System;  
                using Windows.UI.Xaml;
                using Windows.UI.Xaml.Controls.Primitives;
                using Windows.UI.Xaml.Media;
                using Windows.UI.Xaml.Media.Animation;
                namespace Test2 {{
                private class Test2
                {{
                    public void test1()
                    {{
                        var repeatBehavior1 = Microsoft.UI.Xaml.{additionalNamespace}{structType}Helper.{helperName}({arg});
                    }}
                }}}}";

            var expected = new DiagnosticResult
            {
                Id = UWPStructAnalyzer.ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.UWPStruct_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 12, 47)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            VerifyCSharpFix(test, fixTest, null, true);
        }

        //Returns a WUX_Using_CodeFix
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UWPStructCodeFix();
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UWPStructAnalyzer();
        }
    }
}
