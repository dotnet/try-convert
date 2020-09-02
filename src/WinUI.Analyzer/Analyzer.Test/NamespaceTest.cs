using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class NamespaceTest : TestHelper.CodeFixVerifier
    {
        // This contains code to analyze where no diagnostic should be reported

        // 1. No Windows Namespace usings
        private const string NoWUX1 = @"
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

        // 2. No Windows Namespace usings
        private const string NoWUX2 = @"
            using System;
            using Windows.UI.Color;
            namespace FakeNamespace
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
            }";

        // 3. Windows.UI should still be valid on its own
        private const string NoWUX3 = @"
            using System;
            using Windows.UI;
            namespace FakeNamespace
            {
                class Program
                {
                    static void Main(string[] args)
                    {
                    }
                }
            }";

        //5. Should not throw diagnostics in summary pages
        private const string NoWUX5 = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    /// <see cref=""Windows.UI.Xaml.FrameworkElement.ArrangeOverride(Windows.Foundation.Size)"" />
                    static void Main(string[] args)
                    {
                        int i = 0;
                        Console.WriteLine(i++);
                    }
                }
            }";

        //6. Should not throw diagnostics in summary pages
        private const string NoWUX6 = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    /// <summary>
                    /// Measures the child elements of a
                    /// <see cref=""T:WinRTXamlToolkit.Controls.WrapPanel"" /> in anticipation
                    /// of arranging them during the
                    /// <see cref=""Windows.UI.Xaml.FrameworkElement.ArrangeOverride(Windows.Foundation.Size)"" />
                    /// pass.
                    /// </summary>
                    /// <param name=""constraint"">
                    /// The size available to child elements of the wrap panel.
                    /// </param>
                    /// <returns>
                    /// The size required by the
                    /// <see cref=""T:WinRTXamlToolkit.Controls.WrapPanel"" /> and its 
                    /// elements.
                    /// </returns>   
                    static void Main(string[] args)
                    {
                    }
                }
            }";

        // This contains code That should be fixed

        // 1. Regular using CodeFix
        private const string UsingWUX = @"
            using System;
            using Windows.UI.Text.LinkType;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";
        private const string UsingWUXFix = @"
            using System;
            using Microsoft.UI.Text.LinkType;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";

        // 2. Using Directive with Alias and CodeFix
        private const string UsingWUXAlias = @"
            using System;
            using alias = Windows.UI.Xaml;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";
        private const string UsingWUXAliasFix = @"
            using System;
            using alias = Microsoft.UI.Xaml;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";

        // 3. Static using CodeFix
        private const string UsingStaticWUX = @"
            using System;
            using static Windows.UI.Xaml.VisualStateManager;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";
        private const string UsingStaticWUXFix = @"
            using System;
            using static Microsoft.UI.Xaml.VisualStateManager;
            namespace FakeNamespace
            {
                class Program
                {
                }
            }";

        // 4. Inline Using CodeFix before assingment
        private const string UsingInlineWUXBefore = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    Windows.UI.Xaml.Controls.Frame rootFrame = Window.Current.Content as Microsoft.UI.Xaml.Controls.Frame; //Random Comment
                }
            }";
        private const string UsingInlineWUXBeforeFix = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    Microsoft.UI.Xaml.Controls.Frame rootFrame = Window.Current.Content as Microsoft.UI.Xaml.Controls.Frame; //Random Comment
                }
            }";

        // 5. Inline Using CodeFix after equals assign
        private const string UsingInlineWUXAfter = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    Microsoft.UI.Xaml.Controls.Frame rootFrame = Window.Current.Content as Windows.UI.Xaml.Controls.Frame; //Random Comment
                }
            }";
        private const string UsingInlineWUXAfterFix = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    Microsoft.UI.Xaml.Controls.Frame rootFrame = Window.Current.Content as Microsoft.UI.Xaml.Controls.Frame; //Random Comment
                }
            }";

        // 6. Inline static using
        private const string UsingInlineStaticWUX = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    static Windows.UI.Xaml.VisualStateManager visual;
                }
            }";
        private const string UsingInlineStaticWUXFix = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    static Microsoft.UI.Xaml.VisualStateManager visual;
                }
            }";

        // 7. specific replace
        private const string specific1 = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }
            }";
        private const string specific1fix = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    editor.Document.SaveToStream(Microsoft.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                }
            }";

        //8. specific replace
        private const string specific2 = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    editor.Focus(Windows.UI.Xaml.FocusState.Keyboard);
                }
            }";
        private const string specific2fix = @"
            using System;
            namespace FakeNamespace
            {
                class Program
                {
                    editor.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
                }
            }";

        //9. specific replace
        private const string specific3 = @"
            namespace AppUIBasics.ControlPages
            {
                public sealed partial class SplitViewPage : Page
                {
                    private ObservableCollection<NavLink> _navLinks =  new ObservableCollection<NavLink>()
                    {
                        new NavLink() { Label = ""People"", Symbol = Windows.UI.Xaml.Controls.Symbol.People  }
                    }}};";
        private const string specific3fix = @"
            namespace AppUIBasics.ControlPages
            {
                public sealed partial class SplitViewPage : Page
                {
                    private ObservableCollection<NavLink> _navLinks =  new ObservableCollection<NavLink>()
                    {
                        new NavLink() { Label = ""People"", Symbol = Microsoft.UI.Xaml.Controls.Symbol.People  }
                    }}};";

        //9. specific replace
        private const string colors1 = @"
            using Windows.UI
            namespace AppUIBasics.ControlPages
            {
                public sealed partial class SplitViewPage : Page
                {
                    SolidColorBrush s = new SolidColorBrush(Colors.Yellow);
                    }};";
        private const string colors1fix = @"
            namespace AppUIBasics.ControlPages
            {
                public sealed partial class SplitViewPage : Page
                {
                    SolidColorBrush s = new SolidColorBrush(Microsoft.UI.Colors.Yellow);
                    }};";

        //Denotes that method is a data test
        [DataTestMethod]
        [DataRow(""), DataRow(NoWUX1), DataRow(NoWUX2), DataRow(NoWUX3), DataRow(NoWUX5), DataRow(NoWUX6)]
        // Test Method for valid code with no triggered diagnostics
        public void NamespaceNoDiagnostics(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        // TODO: Change all DataRow style tests to look like UWPStructTest.TestReplaceStruct
        [DataTestMethod]
        //[DataRow(UsingWUX, UsingWUXFix, 3, 19),
        //    DataRow(UsingWUXAlias, UsingWUXAliasFix, 3, 27),
        //    DataRow(UsingStaticWUX, UsingStaticWUXFix, 3, 26),
        //    DataRow(UsingInlineWUXBefore, UsingInlineWUXBeforeFix, 7, 21),
        //    DataRow(UsingInlineWUXAfter, UsingInlineWUXAfterFix, 7, 92),
        //    DataRow(UsingInlineStaticWUX, UsingInlineStaticWUXFix, 7, 28),
        //    DataRow(specific1, specific1fix, 7, 50),
        //    DataRow(specific2, specific2fix, 7, 34),
           [ DataRow(colors1, colors1fix, 7, 34)]
        public void NamespaceFixDiagnosticCode(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = NamespaceAnalyzer.ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.Namespace_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", line, column)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            VerifyCSharpFix(test, fixTest, null, true);
        }

        [DataTestMethod]
        [DataRow(specific3, specific3fix, 8, 68)]
        public void NamespaceTypeFixDiagnosticCode(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = NamespaceAnalyzer.TypeName,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.Namespace_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
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
            return new NamespaceCodeFix();
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new NamespaceAnalyzer();
        }
    }
}
