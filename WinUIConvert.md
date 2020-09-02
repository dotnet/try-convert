# Converting To WinUI3 Using Try-Convert

This document details how to use the Try-Convert Tool to convert a C# UWP App to WinUI3 using .NET Native.

## Introduction

- WinUI is a native user experience (UX) framework for both Windows Desktop and UWP applications. WinUI ships as part of the Windows OS. 
[More on WinUI](https://microsoft.github.io/microsoft-ui-xaml/), [Docs](https://docs.microsoft.com/en-us/windows/apps/winui/)
- WinUI3 is the next version of WinUI. It runs on the native Windows 10 UI platform and supports both Windows Desktop and UWP apps. WinUI3 ships as a NuGet package.
[More on WinUI3](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)

Updating a C# UWP App to WinUI3 requires several changes to the .csproj as well as C# code. Running this tool will automate the process. 

## Description
This tool assists with the WinUI --> WinUI3 conversion process for three main scenarios:

- [UWP App --> UWP App](#-UWP-App-->-UWP-App)
- [UWP Class Library --> .NET 5 Multi-Targeting Class Library](#UWP-Library-->-.NET-5.0-Multi-targeting-Library)
- [UWP App --> .NET 5 Desktop App](#UWP-App-->-.Net-5.0-Desktop-App)

This tool assists with the conversion process by first modifying .csproj files to use the new `Microsoft.WinUI` nuget package. It converts nuget packages which are incompatible with the new WinUI3 where possible and removes them otherwise. It also uses [WinUI3 Conversion Analyzers](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) to apply changes to the C# source code.

## How to Build
Clone the repo and run
```
build.cmd
```
To use the tool locally, you need to build it from source. Once it is built, the tool will live under:
```
/artifacts/bin/try-convert/Debug/netcoreapp3.1/try-convert.exe
```
Alternatively, you can look at the following directory and copy that into somewhere else on your machine:
```
mv /artifacts/bin/try-convert/Debug/netcoreapp3.1/publish C:/Users/<user>/try-convert
```

## How to convert to WinUI3

While the converter will create a .csproj.old before attempting any changes, it will modify C# source code files **In Place**!

It is **Highly Recommended** that you use this tool on a project that is under **Source Control**. 

It may be useful to invoke the **-w** flag to ensure the correct path to your .csproj file. 

## UWP App -> UWP App

If you wish to remain on UWP and not upgrade to a .NET 5.0 SDK style project you must invoke the **--keep-uwp** flag.

EX:
```
./try-convert -w C:\PathToYour\file.csproj --keep-uwp
```

try-convert will:
1. Convert and update any NuGet References to WinUI.
2. Remove Incompatible NuGet References.
3. Use the [WinUI3 Analyzers](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) to update C# source code. 

## UWP App -> .Net 5.0 Desktop App

To convert a UWP App to .NET 5.0 SDK style App simply run the tool **Without** the --keep-uwp flag. 

EX:
```
./try-convert -w C:\PathToYour\file.csproj
```

try-convert will:
1. Convert .csproj files to .NET 5.0 SDK
2. Convert and update any NuGet References to WinUI.
3. Remove Incompatible NuGet References.
4. Use the [WinUI3 Analyzers](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) to update C# source code. 
5. Attempt to add a App Package to your solution

## UWP Library -> .NET 5.0 Multi-targeting Library

When converting to a WinUI Library, some source code chages will require #ifDef comments.

To convert a UWP Library to .NET 5.0 SDK style Library simply run the tool **Without** the --keep-uwp flag. 

EX:
```
./try-convert -w C:\PathToYour\file.csproj
```

try-convert will:
1. Convert .csproj files to .NET 5.0 SDK
2. Convert and update any NuGet References to WinUI.
3. Remove Incompatible NuGet References.
4. Use the [WinUI3 Analyzers](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) to update C# source code **With** #ifdef comments enabled.
5. Attempt to add a App Package to your solution


## How the Tool Works

try-convert will make a copy of your .csproj with the .old extension. 
It will then load the project in memory and make modify the package references to include `Microsoft.WinUI`.
If some packages have WinUI3 equivelants it will convert them. 
Some packages may be incompatible with WinUI3 and will be removed.

If you convert to WinUI3 **without** the --keep-uwp flag invoked the tool will also target .Net 5 and upgrade to an SDK style .csproj

#### .csproj Before Running Try-Convert
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.NETCore.UniversalWindowsPlatform" />
    <!-- These packages no longer work -->
    <PackageReference Include="ColorCode.UWP" />
    <PackageReference Include="Win2D.uwp" />
    <!-- This Needs to be Converted-->
    <PackageReference Include="Microsoft.UI.Xaml" />
</ItemGroup>
```
#### .csproj After Running Try-Convert
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.NETCore.UniversalWindowsPlatform" />
    <PackageReference Include="Microsoft.WinUI" />
</ItemGroup>
```
## Source Code Changes

try-convert will analyze and convert C# files and apply the following changes:

### Changes Common to All Projects:
- [Namespace Analyzyer/Codefix](#Namespace-Analyzer/Codefix)
- [EventArgs Analyzer/Codefix](#EventArgs-Analyzer/Codefix)

### Changes Specific to UWP (.NET Native) Projects:
- [UWP Projection Analyzer/Codefix](#UWP-Projection-Analyzer/Codefix)
- [ObservableCollection Analyzer/Codefix](#ObservableCollection-Analyzer/Codefix)
- [UWP Struct Analyzer/Codefix](#UWP-Struct-Analyzer/Codefix)

## Namespace Analyzer/Codefix

- Updates Type Namespaces from `Windows.*` to `Microsoft.*`
- Types moving from `Windows` to `Microsoft`:
    - `Windows.UI.Xaml`
    - `Windows.UI.Colors`
    - `Windows.UI.ColorHelper` 
    - `Windows.UI.Composition`
    - `Windows.UI.Input`
    - `Windows.UI.Text`
    - `Windows.System.DispatcherQueue*` 

#### Namespaces Before Running Analyzers:
```csharp
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
```

#### Namespaces After Running Analyzers:
```csharp
using Microsoft.UI;
using Microsoft.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
```

## EventArgs Analyzer/Codefix

- Converts `App.OnLaunched` Method
    - Two updates need to be made to the `App.OnLaunched` method when converting to WinUI3:
    1. Target `Microsoft.UI.Xaml.LaunchactivatedEvenArgs` as the method parameter type
    2. Instances of the parameter name in the `App.OnLaunched` method body must invoke `UWPLaunchActivatedEventArgs`

#### OnLaunched Method Before Running Analyzers:
```c#
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    await EnsureWindow(args)
}
```

#### OnLaunched Method After Running Analyzers:
```csharp
protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
    await EnsureWindow(args.UWPLaunchActivatedEventArgs)
}
```

## Changes Specific to UWP (.NET Native) Projects:

**Note:** WinUI3 will consume .NET5, and some types and projections curretly baked into the .NET Native runtime are changing: [More Info](https://github.com/microsoft/CsWinRT/issues/77#WinRT-to-.NET-Projections). 

These changes are temporary and only necessary until the move to .NET Core: [More Info](https://github.com/microsoft/ProjectReunion/issues/105).

## UWP Projection Analyzer/Codefix

- Some types are moving in UWP and projects need to target the new projected types.
- .Net Projections moving to `Microsoft.UI.Xaml` :
    - `System.ComponentModel.INotifyPropertyChanged` -> `Microsoft.UI.Xaml.Data.INotifyPropertyChanged`
    - `System.ComponentModel.PropertyChangedEventArgs`-> `Microsoft.UI.Xaml.Data.PropertyChangedEventArgs`
    - `System.Windows.Input.ICommand` -> `Microsoft.UI.Xaml.Input.ICommand`

#### .Net Projections Before Running Analyzers:
```csharp
public sealed partial class CommandBarPage : INotifyPropertyChanged
```

#### .Net Projections After Running Analyzers:
```csharp
 public sealed partial class CommandBarPage : Microsoft.UI.Xaml.Data.INotifyPropertyChanged
```

## ObservableCollection Analyzer/Codefix

- `ObservableCollection<T>` is being removed and users will have to provide their own implementation targeting `INotifyCollectionChanged`.
- If the analyzer cannot find an implementation it will provide its own helper class.
    - `System.Collections.ObjectModel.ObservableCollection` -> `Microsoft.UI.Xaml.Interop.INotifyCollectionChanged`
        
## UWP Struct Analyzer/Codefix

- UWP Projects cannot use struct constructors as they are not being included in the .NET5 projections: 
[More Info](https://github.com/microsoft/CsWinRT/issues/77#WinRT-to-.NET-Projections).
The analyzer replaces these constructors with their associated WinRT Helper classes.
    - `Windows.UI.Xaml.CornerRadius` -> `Microsoft.UI.Xaml.CornerRadiusHelper`
    - `Windows.UI.Xaml.Duration`-> `Microsoft.UI.Xaml.DurationHelper`
    - `Windows.UI.Xaml.GridLength`-> `Microsoft.UI.Xaml.GridLengthHelper`
    - `Windows.UI.Xaml.Thickness`-> `Microsoft.UI.Xaml.ThicknessHelper` ->
    - `Windows.UI.Xaml.Controls.Primitives.GeneratorPosition` -> `Microsoft.UI.Xaml.Controls.Primitives.GeneratorPositionHelper`
    - `Windows.UI.Xaml.Media.Matrix` -> `Microsoft.UI.Xaml.Media.MatrixHelper` 
    - `Windows.UI.Xaml.Media.Animation.KeyTime` -> `Microsoft.UI.Xaml.Media.Animation.KeyTimeHelper`
    - `Windows.UI.Xaml.Media.Animation.RepeatBehavior` -> `Microsoft.UI.Xaml.Media.Animation.RepeatBehaviorHelper`

#### .Net Struct Constructor Before Running Analyzers:
```csharp
CornerRadius c1 = new CornerRadius(4);
CornerRadius c2 = new CornerRadius(4, 2, 2 4);
```

#### .Net Struct Constructor After Running Analyzers:
```csharp
CornerRadius c1  = CornerRadiusHelper.FromUniformRadius(4);
CornerRadius c2  = CornerRadiusHelper.FromRadii(4, 2, 2, 4);
```

## Notes:
