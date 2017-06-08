# ProjectSimplifier
This is a tool that can be used to help with the conversion of old-style csprojs to ones based on the .NET SDK. 

[![Build status](https://ci.appveyor.com/api/projects/status/dcg6k8sca3v83xba?svg=true)](https://ci.appveyor.com/project/SrivatsnNarayanan/msbuildsdkdiffer)

# What does the tool do?
It loads up a given project and evaluates it to get a list of all properties and items. It then replaces the project in memory with a simple .NET SDK based template and then re-evaluates it.
It does the second evaluation in the same project folder so that items that are automatically picked up by globbing will be known as well. It then produces a diff of the two states to identify the following:
- Properties that can now be removed from the project because they are already implicitly defined by the SDK and the project had the default value.
- Properties that need to be kept in the project either because they override the default or it's a property not defined in the SDK.
- Items that can be removed because they are implicitly brought in by globs in the SDK
- Items that need to be changed to the Update syntax because although they're brought by the SDK, there is extra metadata being added.
- Items that need to be kept because theyr are not implicit in the SDK.

# Usage:

From a VS 2017 Developer command prompt
    ProjectSimplifier convert a.csproj -out:b.csproj

From a regular command prompt
    ProjectSimplifier convert a.csproj -out:b.csproj -m:`<path to msbuild.exe>`


Caveats: If your project has custom imports, you might be changing semantics in a very subtle way by moving to the SDK and this tool doesnt know to find those cases.

