// Usings
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

// Arguments
var target = Argument<string>("target", "Default");
var source = Argument<string>("source", null);
var apiKey = Argument<string>("apikey", null);

var version = target.ToLower() == "default"
    ? "2.0.0-Pre" + (EnvironmentVariable("APPVEYOR_BUILD_NUMBER") ?? "0")
    : GetNancyVersion(new FilePath("dependencies/Nancy/src/Nancy/project.json"));

// Variables
var configuration = IsRunningOnWindows() ? "Release" : "MonoRelease";

// Directories
var output = Directory("build");
var outputBinaries = output + Directory("binaries");
var outputBinariesNet452 = outputBinaries + Directory("net452");
var outputBinariesNetstandard = outputBinaries + Directory("netstandard1.6");
var outputPackages = output + Directory("packages");
var outputNuGet = output + Directory("nuget");
var xunit = "test/Nancy.Serialization.JsonNet.Tests/bin/" + configuration + "/net452/unix-x64/dotnet-test-xunit.exe";

/*
/ TASK DEFINITIONS
*/

Task("Default")
    .IsDependentOn("Test")
    .IsDependentOn("Package-NuGet");

Task("Clean")
    .Does(() =>
    {
        // Clean artifact directories.
        CleanDirectories(new DirectoryPath[] {
            output,
            outputBinaries,
            outputPackages,
            outputNuGet,
            outputBinariesNet452,
            outputBinariesNetstandard
        });

        // Clean output directories.
        CleanDirectories("./src/**/" + configuration);
        CleanDirectories("./test/**/" + configuration);
    });

Task("Compile")
    .Description("Builds the solution")
    .IsDependentOn("Clean")
    .IsDependentOn("Update-Version")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
    {
        var projects =
            GetFiles("./src/**/*.xproj") +
            GetFiles("./test/**/*.xproj");

        foreach(var project in projects)
        {
            DotNetCoreBuild(project.GetDirectory().FullPath, new DotNetCoreBuildSettings {
                Configuration = configuration,
                Verbose = false,
                Runtime = IsRunningOnWindows() ? null : "unix-x64"
            });
        }
    });

Task("Package")
    .Description("Zips up the built binaries for easy distribution")
    .IsDependentOn("Publish")
    .Does(() =>
    {
        var package =
            outputPackages + File("Nancy.Serialization.JsonNet-Latest.zip");

        var files =
            GetFiles(outputBinaries.Path.FullPath + "/**/*");

        Zip(outputBinaries, package, files);
    });

Task("Package-NuGet")
    .Description("Generates NuGet packages for each project that contains a nuspec")
    .Does(() =>
    {
        var projects =
            GetFiles("src/**/*.xproj");

        foreach(var project in projects)
        {
            var settings = new DotNetCorePackSettings {
                Configuration = "Release",
                OutputDirectory = outputNuGet
            };

            DotNetCorePack(project.GetDirectory().FullPath, settings);
        }
    });

Task("Publish")
    .Description("Gathers output files and copies them to the output folder")
    .IsDependentOn("Compile")
    .Does(() =>
    {
        // Copy net452 binaries.
        CopyFiles(GetFiles("src/**/bin/" + configuration + "/net452/*.dll")
            + GetFiles("src/**/bin/" + configuration + "/net452/*.xml")
            + GetFiles("src/**/bin/" + configuration + "/net452/*.pdb")
            + GetFiles("src/**/*.ps1"), outputBinariesNet452);

        // Copy netstandard binaries.
        CopyFiles(GetFiles("src/**/bin/" + configuration + "/netstandard1.6/*.dll")
            + GetFiles("src/**/bin/" + configuration + "/netstandard1.6/*.xml")
            + GetFiles("src/**/bin/" + configuration + "/netstandard1.6/*.pdb")
            + GetFiles("src/**/*.ps1"), outputBinariesNetstandard);
    });

Task("Publish-NuGet")
    .Description("Pushes the nuget packages in the nuget folder to a NuGet source. Also publishes the packages into the feeds.")
    .Does(() =>
    {
        if(string.IsNullOrWhiteSpace(apiKey)){
            throw new CakeException("No NuGet API key provided. You need to pass in --apikey=\"xyz\"");
        }

        var packages =
            GetFiles(outputNuGet.Path.FullPath + "/*.nupkg") -
            GetFiles(outputNuGet.Path.FullPath + "/*.symbols.nupkg");

        foreach(var package in packages)
        {
            NuGetPush(package, new NuGetPushSettings {
                Source = source,
                ApiKey = apiKey
            });
        }
    });

Task("Restore-NuGet-Packages")
    .Description("Restores NuGet packages")
    .Does(() =>
    {
        var settings = new DotNetCoreRestoreSettings
        {
            Verbose = false,
            Verbosity = DotNetCoreRestoreVerbosity.Warning,
            Sources = new [] {
                "https://www.myget.org/F/xunit/api/v3/index.json",
                "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json",
                "https://dotnet.myget.org/F/cli-deps/api/v3/index.json",
                "https://api.nuget.org/v3/index.json",
            }
        };

        //Restore at root until preview1-002702 bug fixed
        DotNetCoreRestore("./", settings);
        //DotNetCoreRestore("./src", settings);
        //DotNetCoreRestore("./test", settings);
    });

Task("Test")
    .Description("Executes unit tests")
    .IsDependentOn("Compile")
    .Does(() =>
    {
        if (IsRunningOnWindows())
        {
            var projects =
                GetFiles("./test/**/*.xproj");

            foreach(var project in projects)
            {
                DotNetCoreTest(project.GetDirectory().FullPath, new DotNetCoreTestSettings {
                    Configuration = configuration
                });
            }
        }
        else
        {
            // For when test projects are set to run against netstandard

            // DotNetCoreTest(project.GetDirectory().FullPath, new DotNetCoreTestSettings {
            //   Configuration = configuration,
            //   Framework = "netstandard1.6",
            //   Runtime = "unix-64"
            // });

            var settings = new ProcessSettings {
                Arguments = xunit + " " + "test/Nancy.Serialization.JsonNet.Tests/bin/"+configuration+"/net452/unix-x64/Nancy.Serialization.JsonNet.Tests.dll"
            };

            using(var process = StartAndReturnProcess("mono", settings))
            {
                process.WaitForExit();
                if(process.GetExitCode() != 0)
                {
                    throw new Exception("Nancy.Serialization.JsonNet.Tests failed.");
                }
            }
        }
    });

Task("Update-Version")
    .Description("Update version")
    .Does(() =>
    {
        Information("Setting version to " + version);

        if(string.IsNullOrWhiteSpace(version)) {
            throw new CakeException("No version specified! You need to pass in --targetversion=\"x.y.z\"");
        }

        UpdateProjectJsonVersion(version, "src/Nancy.Serialization.JsonNet/project.json");
    });

/*
/ RUN BUILD TARGET
*/

RunTarget(target);

/*
/ HELPER FUNCTIONS
*/

public static string GetNancyVersion(FilePath filePath)
{
    var project =
        System.IO.File.ReadAllText(filePath.FullPath, Encoding.UTF8);

    return System.Text.RegularExpressions.Regex.Match(project, "\"version\":\\s*\"(.+)\"").Groups[1].ToString();
}

public static void UpdateProjectJsonVersion(string version, string path)
{
    var file =
        new FilePath(path);

    var project =
        System.IO.File.ReadAllText(file.FullPath, Encoding.UTF8);

    var projectVersion =
        new System.Text.RegularExpressions.Regex("(\"version\":\\s*)\".+\"");

    project =
        projectVersion.Replace(project, "$1\"" + version + "\"", 1);

    System.IO.File.WriteAllText(file.FullPath, project, Encoding.UTF8);
}
