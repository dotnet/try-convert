using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace MSBuild.Abstractions
{
    public static class NugetHelpers
    {
        private const string searchUrl = "https://api-v2v3search-0.nuget.org/query?q={0}&prerelease=false&semVerLevel=2.0.0&take=1";

        private static readonly Dictionary<string, string> packageToVersionCache = new Dictionary<string, string>();
        private static readonly HttpClient httpClient = new HttpClient();

        public static async ValueTask<string> GetLatestVersionForPackageNameAsync(string packageName)
        {
            if(packageToVersionCache.TryGetValue(packageName, out var version))
            {
                return version;
            }

            var response = await httpClient.GetAsync(string.Format(searchUrl, packageName));
            var result = await response.Content.ReadAsStreamAsync();
            version = GetVersionFromQueryResponse(result);
            packageToVersionCache[packageName] = version;
            return version;

            static string GetVersionFromQueryResponse(Stream result)
            {
                using var doc = JsonDocument.Parse(result);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var array))
                {
                    foreach (var element in array.EnumerateArray())
                    {
                        return element.GetProperty("version").ToString();
                    }
                }

                return null;
            }
        }
    }
}
