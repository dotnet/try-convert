## Windows Xaml to WinUI project conversion

There are 3 high-level scenarios for converting existing C# UWP projects that use Windows Xaml to C# projects using WinUI3:
 
1. UWP App project to UWP App project
2. UWP App project to Desktop App project (SDK)
3. UWP Class Library to Multitarget Library (SDK)

### Common to All Projects

This section discusses changes to project files and code that are common to the 3 high-level scenarios described above.

 **Project file changes:** Updating PackageReference elements to the new WinUI3 versions:
   - Microsoft.UI.Xaml -> Microsoft.WinUI
   - Microsoft.Xaml.Behaviors.UWP -> Microsoft.Xaml.Behaviors.WinUI
   - Microsoft.Win2D.UWP -> Microsoft.Win2D.WinUI
   - Microsoft.Toolkit.* (v7) -> Microsoft.Toolkit.* (v8)
      - Where most packages are changing names for WinUI, the Windows Community Toolkit packages are just revving the major version

## Convertible packages: 

 - Microsoft.UI.Xaml => Microsoft.WinUI
 - Microsoft.Xaml.Behaviors.UWP=> Microsoft.Xaml.Behaviors.WinUI 
-  Win2D.UWP=> Microsoft.Win2D.WinUI **Currently only works with .NET**
 - ColorCode.UWP=> ColorCode.WinUI 
 - Microsoft.Toolkit=> Microsoft.Toolkit 
- Microsoft.Toolkit.HighPerformance=> Microsoft.Toolkit.HighPerformance 
- Microsoft.Toolkit.Parsers=> Microsoft.Toolkit.Parsers 
- Microsoft.Toolkit.Services=> Microsoft.Toolkit.Services 
- Microsoft.Toolkit.Uwp=> Microsoft.Toolkit.Uwp 
-  Microsoft.Toolkit.Uwp.Connectivity=> Microsoft.Toolkit.Uwp.Connectivity 
 - Microsoft.Toolkit.Uwp.DeveloperTools=> Microsoft.Toolkit.Uwp.DeveloperTools 
 - Microsoft.Toolkit.Uwp.Input.GazeInteraction=> Microsoft.Toolkit.Uwp.Input.GazeInteraction 
 - Microsoft.Toolkit.Uwp.Notifications=> Microsoft.Toolkit.Uwp.Notifications 
 - Microsoft.Toolkit.Uwp.Notifications.JavaScript=> Microsoft.Toolkit.Uwp.Notifications.JavaScript 
 - Microsoft.Toolkit.Uwp.PlatformSpecificAnalyzer=> Microsoft.Toolkit.Uwp.PlatformSpecificAnalyzer 
 - Microsoft.Toolkit.Uwp.UI=> Microsoft.Toolkit.Uwp.UI 
 - Microsoft.Toolkit.Uwp.UI.Animations=> Microsoft.Toolkit.Uwp.UI.Animations 
 - Microsoft.Toolkit.Uwp.UI.Controls=> Microsoft.Toolkit.Uwp.UI.Controls 
 - Microsoft.Toolkit.Uwp.UI.Controls.DataGrid=> Microsoft.Toolkit.Uwp.UI.Controls.DataGrid 
 - Microsoft.Toolkit.Uwp.UI.Controls.Layout=> Microsoft.Toolkit.Uwp.UI.Controls.Layout 
 - Microsoft.Toolkit.Uwp.UI.Media=> Microsoft.Toolkit.Uwp.UI.Media 

 **Source code changes:** 
 
 Namespace conversion:
- `Windows.UI.Xaml`
- `Windows.UI.Colors`
- `Windows.UI.ColorHelper` 
- `Windows.UI.Composition`
- `Windows.UI.Input`
- `Windows.UI.Text`
- `Windows.System.DispatcherQueue*` 

.NET Projection conversion

- System.ComponentModel.INotifyPropertyChanged -> Microsoft.UI.Xaml.Data.INotifyPropertyChanged
- System.ComponentModel.PropertyChangedEventArgs -> Microsoft.UI.Xaml.Data.PropertyChangedEventArgs
- System.Windows.Input.ICommand -> Microsoft.UI.Xaml.Input.ICommand
- System.Collections.ObjectModel.Observablecollection -> Microsoft.UI.Xaml.Interop.INotifyCollectionChanged
    - Note that there needs to be a concrete type that implements this - see the "TestObservableCollection" class in the XamlControlsGallery
- Struct constructors to associated WinRT Helper classes

Complete list of projections: [C#/WinRT issue](https://github.com/microsoft/CsWinRT/issues/77#WinRT-to-.NET-Projections). **You can ignore the following**:
- Foundational Types
- Exceptions
- The Type Type
- ICustomPropertyProvider
- System IO
 
### UWP App project to Desktop App project

1. **Project file changes**: Changing .csproj to target net5.0
   - Use the `Microsoft.NET.Sdk` remove all default property and configurations
   - This conversion should be similar to the one done for MSBuildSdkExtras, except it's using the `Microsoft.NET.Sdk`
2. **Project file changes**: Adding a .wapproj to the .sln
   - Wapproj should be configured to use the .csproj as it's app entry point
   - The appxmanifest from the old uwp .csproj should be used for the .wapproj and have the entry point changed to fullTrustApplication

# Other Issues
- Application.OnLaunched Method needs to start window in a frame, different than original UWP apps. Create an Analyzer/CodeFix to update the C# source code?
- Observablecollection, there is an analyzer to create a class that implements this, but MSBuildWorkspace cannot create new documents, only modify exisiting ones...
   Either 1. extend the Roslyn Codebase, 2. Implement our own Workspace class
- Analyzer fixes in try-convert are slow, is there a way to use batch fixer etc to speed up? May need to implement our own batch fixer
- .wapproj is not complete and needs work to full integrate with an existing project and add a packaging project to the solution.
- .xaml files are not analyzed, use an xmlreader or find/replace in try-convert to fix them?
- obj/*.target files sometimes break try-convert

## Steps after try-convert
 - Does not support ARM architecture
 - clean
 - restore
 - Application InitializeComponent() unecessary
 - Had to disambiguate casts to LaunchActivatedEvenArgs outside of the method. (fully qualify as Windows.ApplicationModel.Activation.LaunchActivatedEventArgs)
 - Some Interfaces in Microsoft.UI.Xaml* Still use Windows.UI.Xaml types and are difficult to debug. Will this be fixed in later previews or does it need an analyzer...?