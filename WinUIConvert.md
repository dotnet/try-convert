# Converting To WinUI3 Using Try-Convert

This document details how to use the Try-Convert Tool to convert a C# UWP App to WinUI3 using .NET Native.

## Introduction
<!-- Use this section to provide background context for the new API(s) 
in this spec. -->
- WinUI2 is an official library with support for native Windows UI elements for Windows apps. 
- WinUI3 Update now supports both Windows Desktop and UWP apps. 

Updating a C# UWP App to WinUI3 can be an involved process and requires several changes to the .csproj as well as C# code. Running this tool will automate the process. 

## Description
This tool assists with the conversion process by first modifying .csproj files to use the new `Microsoft.WinUI` nuget package. It converts nuget packages which are incompatible with the new WinUI3 where possible and removes them otherwise and converts

### .csproj File Examples
Highlighting changes not entire proj file () witre this better

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
This tool also uses the code analyzers packaged as part of `Microsoft.WinUI.Convert` to apply the following changes to C# code. 

- Updates Namespaces for Xaml Types from `Windows.UI.Xaml` to `Microsoft.UI.Xaml`
    - `Windows.UI.Xaml` is now `Microsoft.UI.Xaml`, so `using`s and explicit namespaces need to be updated
- Converts `App.OnLaunched` Method
    - 2 Updates need to be made to the `App.OnLaunched` method when converting to WinUI3
    1. Target `Microsoft.UI.Xaml.LaunchactivatedEvenArgs` as the method parameter type
    2. Instances of the parameter name in the `App.OnLaunched` method body must invoke `UWPLaunchActivatedEventArgs`

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

## How to convert to WinUI3

It may be useful to invoke the -w flag to ensure the correct path to your .csproj file. 

You **Must** invoke `--winui3` flag or the program may fail!

EX:
```
./try-convert -w C:\PathToYour\file.csproj --winui3
```
Opening your project for the first time after conversion will require you to Clean and Rebuild your project. 

## Notes

While the converter will create a .csproj.old before attempting any changes it is highly recommended that you use this tool on a project that is under source control. 