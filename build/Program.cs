using System;
using System.IO;
using System.Linq;
using static Bullseye.Targets;
using static SimpleExec.Command;

const string ArtifactsDir = "artifacts";
const string Clean = "clean";
const string Build = "build";
const string Test = "test";
const string Pack = "pack";
const string Publish = "publish";

Target(Clean, () =>
{
    var filesToDelete = Directory
        .GetFiles(ArtifactsDir, "*.*", SearchOption.AllDirectories)
        .Where(f => !f.EndsWith(".gitignore"));
    foreach (var file in filesToDelete)
    {
        Console.WriteLine($"Deleting file {file}");
        File.SetAttributes(file, FileAttributes.Normal);
        File.Delete(file);
    }

    var directoriesToDelete = Directory.GetDirectories(ArtifactsDir);
    foreach (var directory in directoriesToDelete)
    {
        Console.WriteLine($"Deleting directory {directory}");
        Directory.Delete(directory, true);
    }
});

Target(Build, () => Run("dotnet", "build AWS.Lambda.TestHost.sln -c Release"));

Target(
    Test,
    DependsOn(Build),
    () => Run("dotnet", $"test test/Lambda.TestHost.Tests -c Release -r {ArtifactsDir} --no-build -l trx;LogFileName=AWS.Lambda.TestHost.Tests.xml --verbosity=normal"));

Target(
    Pack,
    DependsOn(Build),
    new[] { "Lambda.ClientExtensions",  "Lambda.TestHost" },
    project => Run("dotnet", $"pack src/{project}/{project}.csproj -c Release -o {ArtifactsDir} --no-build"));

Target(Publish, DependsOn(Pack), () =>
{
    var packagesToPush = Directory.GetFiles(ArtifactsDir, "*.nupkg", SearchOption.TopDirectoryOnly);
    Console.WriteLine($"Found packages to publish: {string.Join("; ", packagesToPush)}");

    var apiKey = Environment.GetEnvironmentVariable("FEEDZ_LOGICALITY_API_KEY");
    if (string.IsNullOrWhiteSpace(apiKey))
    {
        Console.WriteLine("Feedz API Key not available. No packages will be pushed.");
        return;
    }
    Console.WriteLine($"Feedz API Key ({apiKey.Substring(0, 5)}) available. Pushing packages to Feedz...");
    foreach (var packageToPush in packagesToPush)
    {
        Run("dotnet", $"nuget push {packageToPush} -s https://f.feedz.io/logicality/public/nuget/index.json -k {apiKey} --skip-duplicate", noEcho: true);
    }
});

Target("default", DependsOn(Clean, Test, Publish));

RunTargetsAndExit(args);
