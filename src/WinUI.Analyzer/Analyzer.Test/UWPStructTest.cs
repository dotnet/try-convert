using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
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

        // This contains code That should be fixed

        // 1. old Struct
        private const string UsingOld1 = @"
            using System;  
            using Windows.UI.Xaml;
            namespace Test1 {
            private class Test1
            {
                public void test1()
                {
                    var cornerRadius1 = new CornerRadius(2);
                }
            }}";
        private const string UsingOld1Fix = @"
            using System;  
            using Windows.UI.Xaml;
            namespace Test1 {
            private class Test1
            {
                public void test1()
                {
                    var cornerRadius1 = Microsoft.UI.Xaml.CornerRadiusHelper.FromUniformRadius(2);
                }
            }}";

        // 1. old Struct
        private const string UsingOld2 = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Media.Animation;
            namespace Test2 {
            private class Test2
            {
                public void test1()
                {
                    var repeatBehavior1 = new RepeatBehavior(12);
                }
            }}";
        private const string UsingOld2Fix = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Media.Animation;
            namespace Test2 {
            private class Test2
            {
                public void test1()
                {
                    var repeatBehavior1 = Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorHelper.FromCount(12);
                }
            }}";

        // Thickness change
        private const string UsingOld3 = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Data;
            namespace Test3 {
            private class Test3
            {
                public object Convert(object value, Type targetType, object parameter, string language)
                {
                    if (value is double?)
                    {
                        return new Thickness((double)value);
                    }
                    return false;
                }
            }}";
        private const string UsingOld3Fix = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Data;
            namespace Test3 {
            private class Test3
            {
                public object Convert(object value, Type targetType, object parameter, string language)
                {
                    if (value is double?)
                    {
                        return Microsoft.UI.Xaml.ThicknessHelper.FromUniformLength((double)value);
                    }
                    return false;
                }
            }}";

        // as an argument
        private const string UsingOld4 = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Data;
            namespace Test3 {
            private class Test3
            {
                public object Convert(object value, Type targetType, object parameter, string language)
                {
                    return new fakeObj(new Thickness((double)value));
                }
            }}";
        private const string UsingOld4Fix = @"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Data;
            namespace Test3 {
            private class Test3
            {
                public object Convert(object value, Type targetType, object parameter, string language)
                {
                    return new fakeObj(Microsoft.UI.Xaml.ThicknessHelper.FromUniformLength((double)value));
                }
            }}";


        //Denotes that method is a data test
        [DataTestMethod]
        [DataRow(""), DataRow(noUWP1)]
        // Test Method for valid code with no triggered diagnostics
        public void ProjectionsNoDiagnostics(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        [DataRow(UsingOld1, UsingOld1Fix, 9, 41),
            DataRow(UsingOld2, UsingOld2Fix, 10, 43),
            DataRow(UsingOld3, UsingOld3Fix, 12, 32),
            DataRow(UsingOld4, UsingOld4Fix, 10, 40)]
        public void ProjectionFixDiagnosticCode(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = UWPStructAnalyzer.ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.UWPStruct_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", line, column)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            VerifyCSharpFix(test, fixTest, null, true);
        }
        // 1. old Struct
        private string previous = $@"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Media.Animation;
            namespace Test2 {{
            private class Test2
            {{
                public void test1()
                {{
                    var repeatBehavior1 = new {0}({1});
                }}
            }}}}";
        private string codeFix = $@"
            using System;  
            using Windows.UI.Xaml;
            using Windows.UI.Xaml.Media.Animation;
            namespace Test2 {{
            private class Test2
            {{
                public void test1()
                {{
                    var repeatBehavior1 = {0}({1});
                }}
            }}}}";

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
                            new DiagnosticResultLocation("Test0.cs", 10, 47)
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
