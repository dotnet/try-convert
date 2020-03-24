using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;

using Microsoft.Build.Locator;

using NuGet.Versioning;

namespace MSBuild.Conversion.SDK
{
    public static class TargetFrameworkHelper
    {
        public static string FindHighestInstalledTargetFramework(bool? allowPreviews)
        {
            var usePreviewSDK = allowPreviews == true;

            // Finds SDK path
            string sdkPath = null;
            try
            {
                sdkPath = Path.GetFullPath(Path.Combine(MSBuildLocator.QueryVisualStudioInstances().Single().VisualStudioRootPath, "..", ".."));
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to find the .NET SDK on this machine, manually pass '-tfm'");
                throw;
            }

            // Find templates path
            var templatesPath = Path.Combine(sdkPath, "templates");

            // Find highest SDK path (should include previews?)
            var largestVersion = NuGetVersion.Parse("0.0.0.0");
            var templatePath = string.Empty;
            foreach (var templateDirectory in Directory.EnumerateDirectories(templatesPath))
            {
                if (NuGetVersion.TryParse(Path.GetFileName(templateDirectory), out var templatesVersion) &&
                    templatesVersion > largestVersion)
                {
                    if (usePreviewSDK)
                    {
                        largestVersion = templatesVersion;
                        templatePath = Path.GetFullPath(templateDirectory);
                    }
                    else if (!templatesVersion.IsPrerelease)
                    {
                        largestVersion = templatesVersion;
                        templatePath = Path.GetFullPath(templateDirectory);
                    }
                }
            }

            // upzip the common project templates into memory
            var templateNugetPackagePath = Directory.EnumerateFiles(templatePath, "microsoft.dotnet.common.projecttemplates.*.nupkg", SearchOption.TopDirectoryOnly).Single();
            using var templateNugetPackageFile = File.OpenRead(templateNugetPackagePath);
            using var templateNugetPackage = new ZipArchive(templateNugetPackageFile, ZipArchiveMode.Read);
            var templatesJsonFile = templateNugetPackage.Entries
                .Where(x => x.Name.Equals("template.json", StringComparison.OrdinalIgnoreCase) &&
                            x.FullName.Contains("ClassLibrary-CSharp", StringComparison.OrdinalIgnoreCase)).Single();
            using var templatesJson = templatesJsonFile.Open();

            // read the template.json file to see what the tfm is called
            var doc = JsonDocument.ParseAsync(templatesJson).GetAwaiter().GetResult();

            return doc.RootElement.GetProperty("baselines").GetProperty("app").GetProperty("defaultOverrides").GetProperty("Framework").GetString();
        }
    }
}
