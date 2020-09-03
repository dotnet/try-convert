using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class UWPIfDefTest : TestHelper.CodeFixVerifier
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

        // 1. Regular using CodeFix
        private const string UsingOld1 = @"
            using System;  
            using Windows.UI.Xaml;
            namespace Test1 {
            private class oldINotifyPropertyChanged : INotifyPropertyChanged
            {
                void TestAnalyzers()
                {
                    var cornerRadius1 = new CornerRadius(2);
                }
            }";
        private const string UsingOld1Fix = @"
            using System;  
            using Windows.UI.Xaml;
            namespace Test1 {
            private class oldINotifyPropertyChanged : INotifyPropertyChanged
            {
                void TestAnalyzers()
                {
#if  WINDOWS_UWP
            var cornerRadius1 = Microsoft.UI.Xaml.CornerRadiusHelper.FromUniformRadius(2);
#else
                    var cornerRadius1 = new CornerRadius(2);
#endif
        }
            }";


        //Denotes that method is a data test
        [DataTestMethod]
        [DataRow(""), DataRow(noUWP1)]
        // Test Method for valid code with no triggered diagnostics
        public void ProjectionsNoDiagnostics(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        [DataRow(UsingOld1, UsingOld1Fix, 9, 41)]
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

        //Returns a WUX_Using_CodeFix
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new UWPIfDefCodeFix();
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UWPStructAnalyzer();
        }
    }
}
