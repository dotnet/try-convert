# dotnet try-convert

This is a simple tool that will help in migrating .NET Framework projects to .NET Core.

## How to use it

Install it as a global tool here:

```
dotnet tool install -g try-convert
```

Because this is for converting older .NET Framework (Windows) projects, the tool only works on Windows.

If you're using the tool again, make sure you've got the latest release: https://github.com/dotnet/try-convert/releases

```
Usage:
  try-convert [options]

Options:
  -p, --project <P>          The path to a project to convert
  -w, --workspace <W>        The solution or project file to operate on. If a project is not specified, the command will search the current directory for one.
  -m, --msbuild-path <M>     The path to an MSBuild.exe, if you prefer to use that
  --diff-only <DIFF-ONLY>    Produces a diff of the project to convert; no conversion is done
  --no-backup <NO-BACKUP>    Converts projects and does not create a backup of the originals.
```

## Status

| |Unit Tests (Debug)|Unit Tests (Release)|
|---|:--:|:--:|
| ci |[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/try-convert/try-convert-ci?branchName=master&jobName=Windows_NT&configuration=Windows_NT%20Debug&label=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=616&branchName=master)|[![Build Status](https://dev.azure.com/dnceng/public/_apis/build/status/dotnet/try-convert/try-convert-ci?branchName=master&jobName=Windows_NT&configuration=Windows_NT%20Release&label=master)](https://dev.azure.com/dnceng/public/_build/latest?definitionId=616&branchName=master)|
| official | [![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/try-convert/try-convert-official?branchName=master&jobName=Windows_NT&configuration=Windows_NT%20Debug&label=master)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=615&branchName=master)|[![Build Status](https://dev.azure.com/dnceng/internal/_apis/build/status/dotnet/try-convert/try-convert-official?branchName=master&jobName=Windows_NT&configuration=Windows_NT%20Release&label=master)](https://dev.azure.com/dnceng/internal/_build/latest?definitionId=615&branchName=master)|

## How to build

Simple: clone the repo and run

```
build.cmd
```

To use the tool locally, you need to build it from source. Once it's built, the tool will live under:

```
/artifacts/bin/try-convert/Debug/netcoreapp3.0/try-convert.exe
```

Alternatively, you can look at the following directory and copy that into somewhere else on your machine:

```
mv /artifacts/bin/try-convert/Debug/netcoreapp3.0/publish C:/Users/<user>/try-convert
```

You can invoke the tool from the publish directory as well.

## Support policy

**This tool is not supported in any way.** Nobody will be on the hook for fixing any issues with it, nor is anyone who builds this tool obliged to add any requested features.

This is an open source project built by members of the .NET team in their spare time. Although we'll strive to fix issues and add features if people ask for them, the default answer to any issue filed will be, "we'll review a pull request that implements this".

## Who is this tool for?

This tool is for anyone looking to get a little help migrating their projects to .NET Core (or .NET SDK-style projects).

As the name suggests, this tool is not guaranteed to fully convert a project into a 100% working state. The tool is conservative and does as good of a job as it can to ensure that a converted project can still be loaded into Visual Studio and build. However, there are an enormous amount of factors that can result in a project that may not load or build that this tool explicitly does not cover. These include:

* Complex, custom builds that you may have in your solution
* API usage that is incompatible with .NET Core
* Unsupported project types (such as Xamarin, WebForms, or WCF projects)

If the bulk of your codebase is generally capable of moving to .NET Core (such as lots of class libraries with no platform-specific code), then this tool should help quite a bit.

It is highly recommended that you use this tool on a project that is under source control.

## What does the tool do?

It loads a given project and evaluates it to get a list of all properties and items. It then replaces the project in memory with a simple .NET SDK based template and then re-evaluates it.

It does the second evaluation in the same project folder so that items that are automatically picked up by globbing will be known as well. It then applies rules about well-known properties and items, finally producing a diff of the two states to identify the following:

* Properties that can now be removed from the project because they are already implicitly defined by the SDK and the project had the default value
* Properties that need to be kept in the project either because they override the default or are not defined in the SDK.
* Items that can be removed because they are implicitly brought in by globs in the SDK
* Items that need to be changed to the `Update` syntax because although they're brought in by the SDK, there is extra metadata being added.
* Items that need to be kept because they are are not implicit in the SDK.

This diff is used to convert a given project file.

## Attribution

This tool is based on the work of [Srivatsn Narayanan](https://github.com/srivatsn) and his [ProjectSimplifier](https://github.com/srivatsn/ProjectSimplifier) project.
