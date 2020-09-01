using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;

namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class DeprecatedUseTest : TestHelper.CodeFixVerifier
    {
        // This contains code to analyze where a diagnostic should be reported

        // 1. Microsoft.Xaml.Interactivity is unuseable for now
        private const string INC_Report_1 = @"
            using System;
            using Windows.ApplicationModel.Preview.InkWorkspace;
            namespace FakeNamespace
            {
                public class ImageScrollBehavior : DependencyObject, IBehavior
                {
                    public DependencyObject AssociatedObject { get; private set; }
                    public void Attach(DependencyObject associatedObject)
                    {
                        AssociatedObject = associatedObject;
                        if (!GetScrollViewer())
                        {
                            ((ListViewBase)associatedObject).Loaded += ListGridView_Loaded;
                        }
                    }
                }
            }";

        // 2. Inking is no longer compatible
        private const string INC_Report_2 = @"
            using System;
            using Windows.UI.Input.Inking;
            namespace FakeNamespace
            {
                public class Aclass
                {
                }
            }";

        // 3. Inking not compat
        private const string INC_Report_3 = @"
            using System;
            using Windows.ApplicationModel.Preview.InkWorkspace;
            namespace FakeNamespace
            {
                public class Aclass
                {
                }
            }";

        [DataTestMethod]
        [DataRow(INC_Report_1, 3, 19),
            DataRow(INC_Report_2, 3, 19),
            DataRow(INC_Report_3, 3, 19)]
        public void DeprecatedUseIncompatableDiagnosticIsRaised(
           string test,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = DeprecatedUseAnalyzer.ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.Incompatible_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", line, column)
                        }
            };
            VerifyCSharpDiagnostic(test, expected);
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new DeprecatedUseAnalyzer();
        }
    }
}
