using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

namespace OldCsProj
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            TestAnalyzers();
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.UWPLaunchActivatedEventArgs.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.UWPLaunchActivatedEventArgs.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.UWPLaunchActivatedEventArgs.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        private class oldINotifyPropertyChanged : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
        {
            event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
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
        }
        private class oldICommand : Microsoft.UI.Xaml.Input.ICommand
        {
            event EventHandler ICommand.CanExecuteChanged
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

            bool ICommand.CanExecute(object parameter)
            {
                throw new NotImplementedException();
            }

            void ICommand.Execute(object parameter)
            {
                throw new NotImplementedException();
            }
        }

        void TestAnalyzers()
        {
            PropertyChangedEventArgs changeArgs = new Microsoft.UI.Xaml.Data.PropertyChangedEventArgs("tstProp");
            ObservableCollection<string> c = new ObservableCollection<string>(); //Cannot run in try-convert
            var cornerRadius1 = Microsoft.UI.Xaml.CornerRadiusHelper.FromUniformRadius(2);
            var cornerRadius2 = Microsoft.UI.Xaml.CornerRadiusHelper.FromRadii(1,2,3,4);
            var duration = Microsoft.UI.Xaml.DurationHelper.FromTimeSpan(new TimeSpan(33));
            var gridLength1 = Microsoft.UI.Xaml.GridLengthHelper.FromPixels(12);
            var gridLength2 = Microsoft.UI.Xaml.GridLengthHelper.FromValueAndType(1, GridUnitType.Pixel);
            var thickness1 = Microsoft.UI.Xaml.ThicknessHelper.FromUniformLength(2);
            var thickness2 = Microsoft.UI.Xaml.ThicknessHelper.FromLengths(2, 3, 4, 5);
            var generatorPosition = Microsoft.UI.Xaml.Controls.Primitives.GeneratorPositionHelper.FromIndexAndOffset(23, 123);
            var matrix = Microsoft.UI.Xaml.Media.MatrixHelper.FromElements(6, 5, 4, 3, 2, 1);
            var repeatBehavior1 = new RepeatBehavior(12);
            var repeatBehavior2 = new RepeatBehavior(new TimeSpan(10));
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
