using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestHelper;


namespace WinUI.Analyzer.Test
{
    [TestClass]
    public class EventArgsTest : TestHelper.CodeFixVerifier
    {
        // This contains code to analyze where no diagnostic should be reported

        private const string noDiag_1 = @"
        namespace FakeNamespace {
        class Program : Microsoft.UI.Xaml.Application {
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                if (e.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
        }}";

        private const string noDiag_2 = @"
            using Windows.ApplicationModel.Activation;
            using Microsoft.UI.Xaml;
            using Windows.UI.Xaml;
            namespace FakeNamespace
            {
                class Program : Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
                    {
                    }
                }
            }";

        private const string noDiag_3 = @"
            using Windows.ApplicationModel.Activation;
            using Microsoft.UI.Xaml;
            using Windows.UI.Xaml;
            namespace FakeNamespace
            {
                class Program
                {
                    protected void OnLaunched(MyLaunchActivatedEventArgs_NothingToDoWithXaml args)
                    {
                    }
                }
            }";

        private const string noDiag_4 = @"
            using Windows.ApplicationModel.Activation;
            using Microsoft.UI.Xaml;
            using Windows.UI.Xaml;
            namespace FakeNamespace
            {
                class Program
                {
                    protected void OnLaunched()
                    {
                    }
                }
            }";

        // This contains parameter use code That should be fixed

        // 1. App with OnLaunched
        private const string Event_1 = @"
            using Windows.ApplicationModel.Activation;
            using Windows.UI.Xaml;
            namespace FakeNamespace1
            {
                class Program : Application
                {
                    protected override async void OnLaunched(LaunchActivatedEventArgs e)
                    {
                    }
                }
            }";
        private const string EventFix_1 = @"
            using Windows.ApplicationModel.Activation;
            using Windows.UI.Xaml;
            namespace FakeNamespace1
            {
                class Program : Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
                    {
                    }
                }
            }";

        // 2. App with inline Namespace
        private const string Event_2 = @"
            using Windows.UI.Xaml;
            namespace FakeNamespace2
            {
                class Program : Microsoft.UI.Xaml.Application
                {
                    protected override async void OnLaunched(Windows.ApplicationModel.Activation.LaunchActivatedEventArgs args){}
                }
            }";
        private const string EventFix_2 = @"
            using Windows.UI.Xaml;
            namespace FakeNamespace2
            {
                class Program : Microsoft.UI.Xaml.Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args){}
                }
            }";

        // 3. App param with no name
        private const string Event_3 = @"
            using Windows.ApplicationModel.Activation;
            using Windows.UI.Xaml;
            using Microsoft.UI.Xaml;
            namespace FakeNamespace3
            {
                class Program : Application
                {
                    protected override async void OnLaunched(LaunchActivatedEventArgs)
                    {
                    }
                }
            }";
        private const string EventFix_3 = @"
            using Windows.ApplicationModel.Activation;
            using Windows.UI.Xaml;
            using Microsoft.UI.Xaml;
            namespace FakeNamespace3
            {
                class Program : Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs)
                    {
                    }
                }
            }";

        // 4. App with weird comments
        private const string Event_4 = @"
            using Windows.ApplicationModel.Activation;
            using Microsoft.UI.Xaml;
            using Windows.UI.Xaml;
            namespace FakeNamespace4
            {
                class Program : Application
                {
                    protected override async void OnLaunched(LaunchActivatedEventArgs /*ok*/ e)
                    {
                    }
                }
            }";
        private const string EventFix_4 = @"
            using Windows.ApplicationModel.Activation;
            using Microsoft.UI.Xaml;
            using Windows.UI.Xaml;
            namespace FakeNamespace4
            {
                class Program : Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
                    {
                    }
                }
            }";

        // 5. App with alias namespace
        private const string Event_5 = @"
            using Windows.ApplicationModel.Activation;
            using special = Microsoft.UI.Xaml;
            namespace FakeNamespace5
            {
                class Program : Application
                {
                    protected override async void OnLaunched(LaunchActivatedEventArgs e)
                    {
                    }
                }
            }";
        private const string EventFix_5 = @"
            using Windows.ApplicationModel.Activation;
            using special = Microsoft.UI.Xaml;
            namespace FakeNamespace5
            {
                class Program : Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
                    {
                    }
                }
            }";


        // This contains parameter use in method body that should be reported and fixed
        // 1. App with inline Namespace
        private const string EventUse_1 = @"
            namespace Microsoft.UI.Xaml{public class Application{protected virtual void OnLaunched(LaunchActivatedEventArgs args);}}
            namespace FakeNamespace
            {
                class Program : Microsoft.UI.Xaml.Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args){await EnsureWindow(args);}
                }
            }";
        private const string EventUseFix_1 = @"
            namespace Microsoft.UI.Xaml{public class Application{protected virtual void OnLaunched(LaunchActivatedEventArgs args);}}
            namespace FakeNamespace
            {
                class Program : Microsoft.UI.Xaml.Application
                {
                    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args){await EnsureWindow(args.UWPLaunchActivatedEventArgs);}
                }
            }";

        // 2. 
        private const string EventUse_2 = @"
        namespace FakeNamespace{
        class Program : Microsoft.UI.Xaml.Application
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
        }}";
        private const string EventUseFix_2 = @"
        namespace FakeNamespace{
        class Program : Microsoft.UI.Xaml.Application
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                if (e.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                }
                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }
        }}";



        [DataTestMethod]
        [DataRow(""), DataRow(noDiag_1), DataRow(noDiag_2), DataRow(noDiag_3), DataRow(noDiag_4)]
        // Test Method for valid code with no triggered diagnostics
        public void EventArgsNoDiagnostic(string testCode)
        {
            VerifyCSharpDiagnostic(testCode);
        }

        [DataTestMethod]
        // Tests Method parameters for onLaunch are correct type
        [DataRow(Event_1, EventFix_1, 8, 62),
            DataRow(Event_2, EventFix_2, 7, 62),
            DataRow(Event_3, EventFix_3, 9, 62),
            DataRow(Event_4, EventFix_4, 8, 62),
            DataRow(Event_5, EventFix_5, 8, 62)]
        public void EventArgsParamDiagosticIsFixed(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = EventArgsAnalyzer.Param_ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.EventArgs_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("EventArgsTest.cs", line, column)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
            //pass true to allow new errors
            VerifyCSharpFix(test, fixTest, null, true);
        }

        [DataTestMethod]
        // Tests Method parameters are used in method body correctly.
        [DataRow(EventUse_1, EventUseFix_1, 7, 130),
            DataRow(EventUse_2, EventUseFix_2, 11, 21)]
        public void EventArgsArgUseDiagnosticFixed(
           string test,
           string fixTest,
           int line,
           int column)
        {
            var expected = new DiagnosticResult
            {
                Id = EventArgsAnalyzer.Use_ID,
                Message = new LocalizableResourceString(nameof(Analyzer.Resources.EventArgs_MessageFormat), WinUI.Analyzer.Resources.ResourceManager, typeof(Analyzer.Resources)).ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("EventArgsTest.cs", line, column)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
            //pass true to allow new errors
            VerifyCSharpFix(test, fixTest, null, true);
        }

        //Returns a WUX_Using_CodeFix
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EventArgsCodeFix();
        }

        // Returns an analyzer
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EventArgsAnalyzer();
        }
    }
}
