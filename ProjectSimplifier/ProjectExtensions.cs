using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProjectSimplifier
{
    internal static class ProjectExtensions
    {
        public static void LogProjectProperties(this IProject project, string logFileName)
        {
            var lines = new List<string>();
            foreach (var prop in project.Properties.OrderBy(p => p.Name))
            {
                lines.Add($"{prop.Name} = {prop.EvaluatedValue}");
            }
            File.WriteAllLines(logFileName, lines);
        }

        public static string GetTargetFramework(this IProject project)
        {
            var tf = project.GetPropertyValue("TargetFramework");
            if (!string.IsNullOrEmpty(tf))
            {
                return tf;
            }

            var tfi = project.GetPropertyValue("TargetFrameworkIdentifier");
            if (tfi == "")
            {
                throw new InvalidOperationException("TargetFrameworkIdentifier is not set!");
            }

            switch (tfi)
            {
                case ".NETFramework":
                    tf = "net";
                    break;
                case ".NETStandard":
                    tf = "netstandard";
                    break;
                case ".NETCoreApp":
                    tf = "netcoreapp";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown TargetFrameworkIdentifier {tfi}");
            }

            var tfv = project.GetPropertyValue("TargetFrameworkVersion");
            if (tfv == "")
            {
                throw new InvalidOperationException("TargetFrameworkVersion is not set!");
            }

            tf += tfv.TrimStart('v');

            return tf;
        }
    }
}
