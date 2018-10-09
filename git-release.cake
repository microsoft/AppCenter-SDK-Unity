// NOTE: This cannot be run from a Mac.

#addin nuget:?package=Cake.FileHelpers
#addin nuget:?package=Cake.Git
#tool "nuget:?package=gitreleasemanager"

// Task TARGET for build
var TARGET = Argument("target", Argument("t", "Default"));
Task("Default").IsDependentOn("GitRelease");

// Create a tag and release on GitHub
Task("GitRelease")
    .Does(() =>
{
    var specFilePath = GetFiles("UnityPackageSpecs/*.unitypackagespec").First().ToString();
    var publishVersion = XmlPeek(specFilePath, "package/@version");

    var username = "user";
    var password = Argument<string>("GithubToken");
    var owner = "Microsoft";
    var repo = "AppCenter-SDK-Unity";

    // Build a string containing paths to NuGet packages
    var files = GetFiles("output/*.unitypackage");
    var assets = new List<string>();
    Information("Releasing packages:");
    foreach (var file in files)
    {
        if (!file.FullPath.EndsWith("AppCenter-v" + publishVersion + ".unitypackage") &&
            !file.FullPath.EndsWith("AppCenterPush-v" + publishVersion + ".unitypackage"))
        {
            Information(file.FullPath);
            assets.Add(file.FullPath);
        }
    }
    GitReleaseManagerCreate(username, password, owner, repo, new GitReleaseManagerCreateSettings
    {
        Prerelease = false,
        Assets = string.Join(",", assets),
        TargetCommitish = "develop",
        InputFilePath = new FilePath("RELEASE.md"),
        Name = publishVersion
    });
    GitReleaseManagerPublish(username, password, owner, repo, publishVersion);
});

RunTarget(TARGET);
