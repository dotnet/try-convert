using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class UWPProjectionTest : TestHelper.CodeFixVerifier
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

        // 4. Windows.UI.* should still be valid on its own
        private const string NoWUX4 = @"
            using System;
            using Windows.UI.Text;
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
        private const string UsingOld1 = @"
            using System;  
            using System.Collections.Generic;  
            using System.ComponentModel; 
            namespace Test1 {
            private class oldINotifyPropertyChanged : INotifyPropertyChanged
            {
            }}";
        private const string UsingOld1Fix = @"
            using System;  
            using System.Collections.Generic;  
            using System.ComponentModel; 
            namespace Test1 {
            private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
    {
            }}";

        // InotifyPropertyChanged should also change internal class code
        private const string oldInotify2 = @"
        using System;  
        using System.Collections.Generic;  
        using System.ComponentModel; 
        using Windows.UI.Xaml.Data;
        namespace Test2 {
        private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
        {
            event PropertyChangedEventHandler Microsoft.UI.Xaml.Data.INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }
        }}";

        // InotifyPropertyChanged should also change internal class code
        private const string oldInotify2Fix = @"
        using System;  
        using System.Collections.Generic;  
        using System.ComponentModel; 
        using Windows.UI.Xaml.Data;
        namespace Test2 {
        private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
        {
            event Microsoft.UI.Xaml.Data.PropertyChangedEventHandler Microsoft.UI.Xaml.Data.INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }
        }}";

        // InotifyPropertyChanged should also change internal class code
        private const string oldInotify3 = @"
        using System;  
        using System.Collections.Generic;  
        using System.ComponentModel; 
        using Windows.UI.Xaml.Data;
        namespace Test2 {
        private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
        {
            event Microsoft.UI.Xaml.Data.PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }
        }}";

        // InotifyPropertyChanged should also change internal class code
        private const string oldInotify3Fix = @"
        using System;  
        using System.Collections.Generic;  
        using System.ComponentModel; 
        using Windows.UI.Xaml.Data;
        namespace Test2 {
        private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
        {
            event Microsoft.UI.Xaml.Data.PropertyChangedEventHandler Microsoft.UI.Xaml.Data.INotifyPropertyChanged.PropertyChanged
            {
                add
                {
                    throw new NotImplementedException();
                }

                remove
                {
                    throw new NotImplementedException();
                }
            }
        }}";

        // ICommand should also modify Event handler to be generic<object>
        private const string oldICommand1 = @"
        using System;
        using System.Windows.Input;

        namespace AppUIBasics.Common
        {
            public class RelayCommand : Microsoft.UI.Xaml.Input.ICommand
                {
                    public event EventHandler CanExecuteChanged;
                }
        }";

        private const string oldICommand1Fix = @"
        using System;
        using System.Windows.Input;

        namespace AppUIBasics.Common
        {
            public class RelayCommand : Microsoft.UI.Xaml.Input.ICommand
                {
                    public event EventHandler<object> CanExecuteChanged;
                }
        }";


        //Denotes that method is a data test
        [DataTestMethod]
        [DataRow(""), DataRow(noUWP1), DataRow(NoWUX2), DataRow(NoWUX3), DataRow(NoWUX4), DataRow(NoWUX5), DataRow(NoWUX6)]
        // Test Method for valid code with no triggered diagnostics
        public void ProjectionsNoDiagnostics(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        [DataRow(UsingOld1, UsingOld1Fix, 6, 55)]
        public void ProjectionFixDiagnosticCode(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = UWPProjectionAnalyzer.InterfaceID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.UWPProjection_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
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
        [DataRow(oldInotify2, oldInotify2Fix, 9, 19),
            DataRow(oldInotify3, oldInotify3Fix, 9, 70)]
        public void ProjectionFixType(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = UWPProjectionAnalyzer.TypeID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.UWPProjection_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
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
        [DataRow(oldICommand1, oldICommand1Fix, 9, 21)]
        public void ICommandEventFix(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = UWPProjectionAnalyzer.EventID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.UWPProjection_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
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
            return new UWPProjectionCodeFix();
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UWPProjectionAnalyzer();
        }
    }
}
