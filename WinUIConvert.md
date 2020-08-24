# Converting To WinUI3 Using Try-Convert

This document details how to use the Try-Convert Tool to convert a C# UWP App to WinUI3 using .NET Native.

## Introduction

- WinUI is a native user experience (UX) framework for both Windows Desktop and UWP applications. WinUI ships as part of the Windows OS. 
[More on WinUI](https://microsoft.github.io/microsoft-ui-xaml/), [Docs](https://docs.microsoft.com/en-us/windows/apps/winui/)
- WinUI3 is the next version of WinUI. It runs on the native Windows 10 UI platform and supports both Windows Desktop and UWP apps. WinUI3 ships as a NuGet package.
[More on WinUI3](https://docs.microsoft.com/en-us/windows/apps/winui/winui3/)

Updating a C# UWP App to WinUI3 can be an involved process and requires several changes to the .csproj as well as C# code. Running this tool will automate the process. 

## Description
This tool assists with the conversion process by first modifying .csproj files to use the new `Microsoft.WinUI` nuget package. It converts nuget packages which are incompatible with the new WinUI3 where possible and removes them otherwise. It also uses [WinUI3 Conversion Analyzers](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) to apply changes to the C# source code.

### .csproj File Examples

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

**See the [WinUI3 Docs](https://github.com/microsoft/microsoft-ui-xaml/blob/master/docs/preview_conversion_analyzer.md) for more information on the Conversion Analyzers**

This tool also uses the code analyzers packaged as part of `Microsoft.WinUI.Convert` to apply the following changes to C# code. 

- Updates Namespaces for Xaml Types from `Windows.UI.Xaml` to `Microsoft.UI.Xaml`
    - `Windows.UI.Xaml` is now `Microsoft.UI.Xaml`, so `using`s and explicit namespaces need to be updated
- Converts `App.OnLaunched` Method
    - 2 Updates need to be made to the `App.OnLaunched` method when converting to WinUI3
    1. Target `Microsoft.UI.Xaml.LaunchactivatedEvenArgs` as the method parameter type
    2. Instances of the parameter name in the `App.OnLaunched` method body must invoke `UWPLaunchActivatedEventArgs`

### UWP Only:
- Some types are moving in UWP and projects need to target the new projected types.
- `ObservableCollection<T>` is being removed try-convert will provide its own helper class.
    - `System.Collections.ObjectModel.ObservableCollection` -> `Microsoft.UI.Xaml.Interop.INotifyCollectionChanged`
- UWP Projects cannot use struct constructors. 
The analyzer replaces these constructors with their associated WinRT Helper classes.

### C# Examples

#### Namespaces Before Running Try-Convert:
```csharp
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
```

#### Namespaces Before Running Try-Convert:
```c#
protected override async void OnLaunched(LaunchActivatedEventArgs args)
{
    await EnsureWindow(args)
}
```

#### Namespaces After Running Try-Convert:
```csharp
protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
{
    await EnsureWindow(args.UWPLaunchActivatedEventArgs)
}
```

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
You can invoke the tool from the publish directory as well.

# How to convert to WinUI3

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

## UWP Library -> .NET 5.0 Multitarget Library

When converting to a WinUI Library, some source code chages will require #ifDef comments.

To convert a UWP Library to .NET 5.0 SDK style App simply run the tool **Without** the --keep-uwp flag. 

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

## Notes
