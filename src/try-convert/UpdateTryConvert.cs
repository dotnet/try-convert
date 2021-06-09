using System;
using System.Diagnostics;
namespace MSBuild.Conversion
{
    public static class UpdateTryConvert
    {
        public static void Update()
        {
            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.FileName = "dotnet";
                process.StartInfo.Arguments = "tool update --global try-convert";
                process.Start();
            }
        }
    }
}
