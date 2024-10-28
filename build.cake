// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#addin nuget:?package=Cake.FileHelpers&version=2.0.0
#addin nuget:?package=Cake.AzureStorage
#addin nuget:?package=Cake.Xcode
#addin nuget:?package=Cake.Json&version=3.0.1
#addin nuget:?package=Cake.Http&version=1.3.0
#load "utility.cake"
#load "nuget-tools.cake"

using Path = System.IO.Path;
using System;
using System.Net;
using System.Threading;

// Native SDK versions
const string AndroidSdkVersion = "4.4.2";
const string IosSdkVersion = "4.4.1";
const string UwpSdkVersion = "4.5.0";

// URLs for downloading binaries.
/*
 * Read this: http://www.mono-project.com/docs/faq/security/.
 * On Windows,
 *     you have to do additional steps for SSL connection to download files.
 *     http://stackoverflow.com/questions/4926676/mono-webrequest-fails-with-https
 *     By running mozroots and install part of Mozilla's root certificates can make it work.
 */

const string SdkStorageUrl = "https://mobilecentersdkdev.blob.core.windows.net/sdk/";
const string AndroidUrl = SdkStorageUrl + "AppCenter-SDK-Android-" + AndroidSdkVersion + ".zip";
const string IosUrl = SdkStorageUrl + "AppCenter-SDK-Apple-" + IosSdkVersion + ".zip";
const string UwpUrl = SdkStorageUrl + "AppCenter-SDK-Unity-UWP-" + UwpSdkVersion + ".zip";
const string AndroidRepoUrl = "https://github.com/microsoft/appcenter-sdk-android.git";
const string AppleRepoUrl = "https://github.com/microsoft/appcenter-sdk-apple.git";
const string DotNetRepoUrl = "https://github.com/microsoft/appcenter-sdk-dotnet.git";

var ReposToUpdate = new List<string>()
{ 
    AndroidRepoUrl,
    AppleRepoUrl,
    DotNetRepoUrl
};

var AppCenterModules = new []
{
    new AppCenterModule("appcenter-release.aar", "AppCenter.framework", "Microsoft.AppCenter", "Core"),
    new AppCenterModule("appcenter-analytics-release.aar", "AppCenterAnalytics.framework", "Microsoft.AppCenter.Analytics", "Analytics"),
    new AppCenterModule("appcenter-distribute-release.aar", new[] { "AppCenterDistribute.framework", "AppCenterDistributeResources.bundle" }, "Microsoft.AppCenter.Distribute", "Distribute"),
    new AppCenterModule("appcenter-crashes-release.aar", "AppCenterCrashes.framework", "Microsoft.AppCenter.Crashes", "Crashes")
};

var ExternalUnityPackages = new []
{
    // From https://github.com/googlesamples/unity-jar-resolver#getting-started
    // Import the play-services-resolver-*.unitypackage into your plugin project <...> ensuring that you add the -gvh_disable option.
    // You must specify the -gvh_disable option in order for the Version Handler to work correctly!
    new ExternalUnityPackage($"play-services-resolver-{ExternalUnityPackage.VersionPlaceholder}.0.unitypackage",
                             "1.2.135",
                             $"https://github.com/googlesamples/unity-jar-resolver/raw/v{ExternalUnityPackage.VersionPlaceholder}/{ExternalUnityPackage.NamePlaceholder}",
                             "-gvh_disable")
};

// Unity requires a specific NDK version for building Android with IL2CPP.
// Download from a link here: https://developer.android.com/ndk/downloads/older_releases.html
// Unity 2018.4.18 requires NDK r16b.
// The destination for the NDK download.
const string NdkFolder = "android_ndk";

// Task TARGET for build
var Target = Argument("target", Argument("t", "Default"));

// App Center module class definition.
class AppCenterModule
{
    public string AndroidModule { get; private set; }
    public string[] IosModules { get; private set; }
    public string DotNetModule { get; private set; }
    public string Moniker { get; private set; }
    public bool UWPHasNativeCode { get; private set; }
    public string[] NativeArchitectures { get; private set; }

    public AppCenterModule(string android, string ios, string dotnet, string moniker, bool hasNative = false) :
        this(android, new[] { ios }, dotnet, moniker, hasNative)
    {
    }

    public AppCenterModule(string android, string[] ios, string dotnet, string moniker, bool hasNative = false)
    {
        AndroidModule = android;
        IosModules = ios;
        DotNetModule = dotnet;
        Moniker = moniker;
        UWPHasNativeCode = hasNative;
        if (hasNative)
        {
            NativeArchitectures = new string[] {"x86", "x64", "arm", "arm64"};
        }
    }
}

class ExternalUnityPackage
{
    public static string NamePlaceholder = "<name>";
    public static string VersionPlaceholder = "<version>";

    public string Name { get; private set; }
    public string Version { get; private set; }
    public string Url { get; private set; }
    public string AdditionalImportArgs { get; private set; }

    public ExternalUnityPackage(string name, string version, string url, string additionalImportArgs = "")
    {
        AdditionalImportArgs = additionalImportArgs;
        Version = version;
        Name = name.Replace(VersionPlaceholder, Version);
        Url = url.Replace(NamePlaceholder, Name).Replace(VersionPlaceholder, Version);
    }
}

// Spec files can have up to one dependency.
class UnityPackage
{
    private string _packageName;
    private string _packageVersion;
    private List<string> _includePaths = new List<string>();

    public UnityPackage(string specFilePath)
    {
        var result = AddFilesFromSpec(specFilePath);
        if (!result)
        {
            throw new Exception("Failed to form unity package.");
        }
    }

    private bool AddFilesFromSpec(string specFilePath)
    {
        var needsCore = Statics.Context.XmlPeek(specFilePath, "package/@needsCore") == "true";
        if (needsCore)
        {
            var specFileDirectory = Path.GetDirectoryName(specFilePath);
            if (!AddFilesFromSpec(specFileDirectory + "/AppCenter.unitypackagespec"))
            {
                return false;
            }
        }
        _packageName = Statics.Context.XmlPeek(specFilePath, "package/@name");
        _packageVersion = Statics.Context.XmlPeek(specFilePath, "package/@version");
        if (_packageName == null || _packageVersion == null)
        {
            Statics.Context.Error("Invalid format for UnityPackageSpec file '" + specFilePath + "': missing package name or version");
            return false;
        }

        var xpathPrefix = "/package/include/file[";
        var xpathSuffix= "]/@path";

        var lastPath = Statics.Context.XmlPeek(specFilePath, xpathPrefix + "last()" + xpathSuffix);
        var currentIdx = 1;
        var currentPath = Statics.Context.XmlPeek(specFilePath, xpathPrefix + currentIdx++ + xpathSuffix);

        if (currentPath != null)
        {
            _includePaths.Add(currentPath);
        }
        while (currentPath != lastPath)
        {
            currentPath = Statics.Context.XmlPeek(specFilePath, xpathPrefix + currentIdx++ + xpathSuffix);
            _includePaths.Add(currentPath);
        }
        return true;
    }

    public void CreatePackage(string targetDirectory)
    {
        // From https://github.com/googlesamples/unity-jar-resolver#getting-started
        // Export your plugin <...> ensuring that you <...> Add the -gvh_disable option.
        // You must specify the -gvh_disable option in order for the Version Handler to work correctly!
        var args = "-gvh_disable -exportPackage ";
        foreach (var path in _includePaths)
        {
            args += " " + path;
        }
        var fullPackageName = _packageName + "-v" + _packageVersion + ".unitypackage";
        args += " " + targetDirectory + "/" + fullPackageName;
        var result = ExecuteUnityCommand(args);
        if (result != 0)
        {
            Statics.Context.Error("Something went wrong while creating Unity package '" + fullPackageName + "'");
        }
    }

    public void CopyFiles(DirectoryPath targetDirectory)
    {
        foreach (var path in _includePaths)
        {
            if (Statics.Context.DirectoryExists(path))
            {
                Statics.Context.CopyDirectory(path, targetDirectory.Combine(path));
            }
            else
            {
                Statics.Context.CopyFile(path, targetDirectory.CombineWithFilePath(path));
            }
        }
    }
}

// Downloading Android binaries.
Task("Externals-Android").Does(() =>
{
    var externalsDirectory = "externals/android/";
    var outputDirectory = "Assets/AppCenter/Plugins/Android/";
    var zipFilePath = externalsDirectory + "android.zip";
    CleanDirectory(externalsDirectory);
    EnsureDirectoryExists(outputDirectory);

    // Download zip file.
    var authParams = Argument("StorageAuthParams", EnvironmentVariable("STORAGE_AUTH_PARAMS"));
    var artifactUrl = $"{AndroidUrl}{authParams}";
    Information($"Downloading Android libraries from {artifactUrl}...");
    DownloadFile(artifactUrl, zipFilePath);
    Information($"Unzipping Android libraries from \"{zipFilePath}\" to \"{externalsDirectory}\".");
    Unzip(zipFilePath, externalsDirectory);

    // Copy files.
    foreach (var module in AppCenterModules)
    {
        Information($"Copying module \"{module.Moniker}\"...");
        var files = GetFiles($"{externalsDirectory}*/{module.AndroidModule}");
        CopyFiles(files, outputDirectory);
    }
}).OnError(HandleError);

// Downloading iOS binaries.
Task("Externals-Ios")
    .Does(() =>
{
    var externalsDirectory = "externals/ios/";
    var outputDirectory = "Assets/AppCenter/Plugins/iOS/";
    var zipFilePath = externalsDirectory + "ios.zip";
    CleanDirectory(externalsDirectory);
    EnsureDirectoryExists(outputDirectory);

    // Download zip file containing AppCenter frameworks.
    var authParams = Argument("StorageAuthParams", EnvironmentVariable("STORAGE_AUTH_PARAMS"));
    var artifactUrl = $"{IosUrl}{authParams}";
    Information($"Downloading iOS frameworks from {artifactUrl}...");
    DownloadFile(artifactUrl, zipFilePath);
    Information($"Unzipping iOS frameworks from \"{zipFilePath}\" to \"{externalsDirectory}\".");
    Unzip(zipFilePath, externalsDirectory);

    // Copy files.
    foreach (var module in AppCenterModules)
    {
        Information($"Copying module \"{module.Moniker}\"...");
        foreach (var iosModule in module.IosModules)
        {
            var destinationDirectory = $"{outputDirectory}{module.Moniker}/{iosModule}";
            DeleteDirectoryIfExists(destinationDirectory);
            MoveDirectory(externalsDirectory + "AppCenter-SDK-Apple/iOS/" + iosModule, destinationDirectory);
        }
    }
}).OnError(HandleError);

// Downloading UWP binaries.
Task("Externals-Uwp")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
{
    var externalsDirectory = "externals/uwp/";
    var outputDirectory = "Assets/AppCenter/Plugins/WSA/";
    var zipFilePath = externalsDirectory + "uwp.zip";
    CleanDirectory(externalsDirectory);
    EnsureDirectoryExists(outputDirectory);

    // Downloading files.
    var authParams = Argument("StorageAuthParams", EnvironmentVariable("STORAGE_AUTH_PARAMS"));
    var artifactUrl = $"{UwpUrl}{authParams}";
    Information($"Downloading UWP packages from {artifactUrl}...");
    DownloadFile(artifactUrl, zipFilePath);

    // Unzipping files.
    Information($"Unzipping UWP packages from \"{zipFilePath}\" to \"{externalsDirectory}\".");
    Unzip(zipFilePath, externalsDirectory);
    foreach (var module in AppCenterModules)
    {
        if (module.Moniker == "Distribute")
        {
            Warning("Skipping 'Distribute' for UWP.");
            continue;
        }
        if (module.Moniker == "Crashes")
        {
            Warning("Skipping 'Crashes' for UWP.");
            continue;
        }
        Information($"Copying module \"{module.Moniker}\"...");

        // Prepare folders.
        var destination = $"{outputDirectory}{module.Moniker}/";
        EnsureDirectoryExists(destination);
        DeleteFiles(destination + "*.dll");
        DeleteFiles(destination + "*.winmd");

        // Copy files.
        var files = GetFiles($"{externalsDirectory}{module.DotNetModule}.dll");
        CopyFiles(files, destination);
    }
}).OnError(HandleError);

// Builds the ContentProvider for the Android package and puts it in the
// proper folder.
Task("BuildAndroidContentProvider").Does(() =>
{
    // Folder and script locations
    var appName = "AppCenterLoaderApp";
    var libraryName = "appcenter-loader";
    BuildAndroidLibrary(appName, libraryName);

    // This is a workaround for NDK build making an error where it claims
    // it can't find the built .so library (which is built successfully).
    // This error is breaking the CI build on Windows. If you are seeing this,
    // most likely we haven't found a fix and this is an NDK bug.
    if (!IsRunningOnWindows())
    {
        appName = "BreakpadSupport";
        BuildAndroidLibrary(appName, "", false);
        if (GetFiles("Assets/Plugins/Android/*/libPuppetBreakpad.so").Count() == 0)
        {
            throw new Exception("Building breakpad library failed.");
        }
    }
}).OnError(HandleError);

void BuildAndroidLibrary(string appName, string libraryName, bool copyBinary = true)
{
    var libraryFolder = Path.Combine(appName, libraryName);
    var gradleScript = Path.Combine(libraryFolder, "build.gradle");

    // Compile the library
    var gradleWrapper = Path.Combine(appName, "gradlew");
    if (IsRunningOnWindows())
    {
        gradleWrapper += ".bat";
    }
    var fullArgs = "-b " + gradleScript + " assembleRelease";
    var result = StartProcess(gradleWrapper, fullArgs);
    if (copyBinary)
    {
        // We check the result of the build only here because we need to check build
        // result only in case we build native binary.
        // In another case, when we build breakpad, we don't check it
        // Due to the known issue that breakpad build always reports false failure.
        if (result != 0)
        {
            throw new Exception("Failed to build android native library. Gradle result code: " + result);
        }
        // Source and destination of generated aar
        var aarName = libraryName + "-release.aar";
        var aarSource = Path.Combine(libraryFolder, "build/outputs/aar/" + aarName);
        var aarDestination = "Assets/AppCenter/Plugins/Android";

        // Move the .aar to destination folder.
        DeleteFileIfExists(Path.Combine(aarDestination, aarName));
        MoveFileToDirectory(aarSource, aarDestination);
    }
}

// Install Unity Editor for Windows
Task("Install-Unity-Windows")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
{
    string unityDownloadUrl = EnvironmentVariable("EDITOR_URL_WIN");
    string il2cppSupportDownloadUrl = EnvironmentVariable("IL2CPP_SUPPORT_URL");

    var URLs = new []
    { 
        unityDownloadUrl,
        il2cppSupportDownloadUrl
    };

    foreach (var url in URLs)
    {
        var fileName = Path.GetFileName(url);
        Information($"Downloading component {fileName} ...");
        DownloadFile(url, $"./{fileName}");
        Information($"Installing {fileName}  ...");

        var result = StartProcess($"./{fileName}", "/S");
        if (result != 0)
        {
            throw new Exception($"Failed to install {fileName}");
        }
    }
}).OnError(HandleError);

// Downloading UWP IL2CPP dependencies.
Task("Externals-Uwp-IL2CPP-Dependencies")
    .WithCriteria(IsRunningOnWindows)
    .IsDependentOn("BuildNewtonsoftJson")
    .Does (async () =>
{
    var targetPath = "Assets/AppCenter/Plugins/WSA/IL2CPP";
    EnsureDirectoryExists(targetPath);
    EnsureDirectoryExists(targetPath + "/ARM");
    EnsureDirectoryExists(targetPath + "/ARM64");
    EnsureDirectoryExists(targetPath + "/X86");
    EnsureDirectoryExists(targetPath + "/X64");

    foreach (var dependency in UwpIL2CPPDependencies)
    {
        await ProcessDependency(dependency, targetPath);
    }

    // Process UWP IL2CPP dependencies.
    Information("Processing UWP IL2CPP dependencies. This could take a minute.");
    var result = ExecuteUnityCommand("-executeMethod AppCenterPostBuild.ProcessUwpIl2CppDependencies");
    if (result != 0)
    {
        throw new Exception("Something went wrong while executing post build script. Unity result code: " + result);
    }
}).OnError(HandleError);

Task("RestorePackagesForNewtonsoftJson")
    .Does(() =>
{
    NuGetRestore("./Modules/Newtonsoft.Json-for-Unity/Src/Newtonsoft.Json/Newtonsoft.Json.csproj");
});

Task("BuildNewtonsoftJson")
    .IsDependentOn("RestorePackagesForNewtonsoftJson")
    .WithCriteria(IsRunningOnWindows)
    .Does(() =>
{
    var outputDirectory = "Assets/AppCenter/Plugins/WSA/IL2CPP";
    var projectPath = "Modules/Newtonsoft.Json-for-Unity/Src/Newtonsoft.Json";
    var assemblyName = "Newtonsoft.Json.dll";
    var assemblyPath = $"{projectPath}/bin/Release/unity-aot/{assemblyName}";

    Information("Building Newtonsoft.Json project...");
    MSBuild(Path.Combine(projectPath, "Newtonsoft.Json.csproj"), c => c
        .WithProperty("UnityBuild", "AOT")
        .SetConfiguration("Release"));

    Information($"Moving Newtonsoft.Json to {outputDirectory}...");
    DeleteFileIfExists(Path.Combine(outputDirectory, assemblyName));
    MoveFileToDirectory(assemblyPath, outputDirectory);
}).OnError(HandleError);

// Download and install all external Unity packages required.
Task("Externals-Unity-Packages").Does(() =>
{
    var directoryPath = new DirectoryPath("externals/unity-packages");
    CleanDirectory(directoryPath);
    foreach (var package in ExternalUnityPackages)
    {
        var destination = directoryPath.CombineWithFilePath(package.Name);
        DownloadFile(package.Url, destination);
        Information("Importing package " + package.Name + ". This could take a minute.");
        var command = package.AdditionalImportArgs + " -importPackage " + destination.FullPath;
        var result = ExecuteUnityCommand(command);
        if (result != 0)
        {
            throw new Exception("Something went wrong while importing packages. Unity result code: " + result);
        }
    }
}).OnError(HandleError);

// Add App Center packages to demo app.
Task("AddPackagesToDemoApp")
    .IsDependentOn("CreatePackages")
    .Does(()=>
{
    var specFiles = GetFiles("UnityPackageSpecs/*.unitypackagespec");
    foreach (var spec in specFiles)
    {
        Information("Add files from " + spec);
        var package = new UnityPackage(spec.FullPath);
        package.CopyFiles("AppCenterDemoApp");
    }
}).OnError(HandleError);

// Remove package files from demo app.
Task("RemovePackagesFromDemoApp").Does(() =>
{
    DeleteDirectoryIfExists("AppCenterDemoApp/Assets/AppCenter");
    DeleteDirectoryIfExists("AppCenterDemoApp/Assets/Plugins");
}).OnError(HandleError);

// Create a common externals task depending on platform specific ones
// NOTE: It is important to execute Externals-Uwp-IL2CPP-Dependencies *last*
// or steps that run Unity commands might cause the *.meta files to be deleted!
// (Unity deletes meta data files when it is opened if the corresponding files are not on disk.)
Task("Externals")
    .IsDependentOn("Externals-Uwp")
    .IsDependentOn("Externals-Ios")
    .IsDependentOn("Externals-Android")
    .IsDependentOn("BuildAndroidContentProvider")
    .IsDependentOn("Externals-Uwp-IL2CPP-Dependencies")
    .IsDependentOn("Externals-Unity-Packages")
    .Does(() =>
{
    DeleteDirectoryIfExists("externals");
});

// Creates Unity packages corresponding to all ".unitypackagespec" files
// in "UnityPackageSpecs" folder.
Task("Package").Does(() =>
{
    // Remove AndroidManifest.xml
    var path = "Assets/Plugins/Android/appcenter/AndroidManifest.xml";
    DeleteFileIfExists(path);

    // Store packages in a clean folder.
    const string outputDirectory = "output";
    CleanDirectory(outputDirectory);
    var specFiles = GetFiles("UnityPackageSpecs/*.unitypackagespec");
    foreach (var spec in specFiles)
    {
        var package = new UnityPackage(spec.FullPath);
        package.CreatePackage(outputDirectory);
    }
});

Task("PrepareAssets").IsDependentOn("Externals");

// Creates Unity packages corresponding to all ".unitypackagespec" files
// in "UnityPackageSpecs" folder (and downloads binaries)
Task("CreatePackages").IsDependentOn("PrepareAssets").IsDependentOn("Package");

// Builds the puppet applications and throws an exception on failure.
Task("BuildPuppetApps")
    .IsDependentOn("PrepareAssets")
    .Does(() =>
{
    BuildApps("Puppet");
}).OnError(HandleError);

// Builds the demo applications and throws an exception on failure.
Task("BuildDemoApps")
    .IsDependentOn("AddPackagesToDemoApp")
    .Does(() =>
{
    BuildApps("Demo", "AppCenterDemoApp");
}).OnError(HandleError);

// Downloads the NDK from the specified location.
Task("DownloadNdk").Does(() =>
{
    var ndkUrl = EnvironmentVariable("ANDROID_NDK_URL");
    if (string.IsNullOrEmpty(ndkUrl))
    {
        throw new Exception("Ndk Url is empty string or null");
    }
    var zipDestination = Statics.TemporaryPrefix + "ndk.zip";

    // Download required NDK
    DownloadFile(ndkUrl, zipDestination);

    // Something is buggy about the way Cake unzips, so use shell on mac
    if (IsRunningOnUnix())
    {
        CleanDirectory(NdkFolder);
        var result = StartProcess("unzip", new ProcessSettings{ Arguments = $"{zipDestination} -d {NdkFolder}"});
        if (result != 0)
        {
            throw new Exception("Failed to unzip ndk.");
        }
    }
    else
    {
        Unzip(zipDestination, NdkFolder);
    }
}).OnError(HandleError);

void BuildApps(string type, string projectPath = ".")
{
    if (Statics.Context.IsRunningOnUnix())
    {
        VerifyIosAppsBuild(type, projectPath);
        VerifyAndroidAppsBuild(type, projectPath);
    }
    else
    {
        VerifyWindowsAppsBuild(type, projectPath);
    }
}

void VerifyIosAppsBuild(string type, string projectPath)
{
    VerifyAppsBuild(type, "ios", projectPath,
    // From Unity 2018.3 changelog: "iOS: Marked Mono scripting backend as deprecated."
    new string[] { "IosIl2CPP" },
    outputDirectory =>
    {
        var directories = GetDirectories(outputDirectory + "/*/*.xcodeproj");
        if (directories.Count == 0)
        {
            throw new Exception("No ios projects found in directory '" + outputDirectory + "'");
        }
        var xcodeProjectPath = directories.Single();
        Statics.Context.Information("Attempting to build '" + xcodeProjectPath.FullPath + "'...");
        BuildXcodeProject(xcodeProjectPath.FullPath);
        Statics.Context.Information("Successfully built '" + xcodeProjectPath.FullPath + "'");
    });
}

void VerifyAndroidAppsBuild(string type, string projectPath)
{
    var extraArgs = "";
    if (DirectoryExists(NdkFolder))
    {
        var absoluteNdkFolder = Statics.Context.MakeAbsolute(Statics.Context.Directory(NdkFolder));
        extraArgs += "-NdkLocation \"" + absoluteNdkFolder + "\"";
    }
    VerifyAppsBuild(type, "android", projectPath,
    new string[] { "AndroidIl2CPP" },
    outputDirectory =>
    {
        // Verify that an APK was generated.
        if (Statics.Context.GetFiles(outputDirectory + "/*.apk").Count == 0)
        {
            throw new Exception("No apk found in directory '" + outputDirectory + "'");
        }
        Statics.Context.Information("Found apk.");
    }, extraArgs);
}

void VerifyWindowsAppsBuild(string type, string projectPath)
{
    VerifyAppsBuild(type, "wsaplayer", projectPath,
    new string[] { "WsaIl2CPPD3D" },
    outputDirectory =>
    {
        Statics.Context.Information("Verifying app build in directory: " + outputDirectory);
        var slnFiles = GetFiles(outputDirectory + "/*/*.sln");
        if (slnFiles.Count() == 0)
        {
            throw new Exception("No .sln files found in the following directory and all it's subdirectories: " + outputDirectory);
        }
        if (slnFiles.Count() > 1)
        {
            throw new Exception(string.Format("Multiple .sln files found in directory {0}: {1}", outputDirectory, string.Join(", ", slnFiles)));
        }
        var solutionFilePath = slnFiles.Single();
        Statics.Context.Information("Attempting to build '" + solutionFilePath.ToString() + "'...");
        Statics.Context.MSBuild(solutionFilePath.ToString(), c => c
            .SetConfiguration("Master")
            .WithProperty("Platform", "x86")
            .SetVerbosity(Verbosity.Minimal)
            .SetMSBuildPlatform(MSBuildPlatform.x86));
        Statics.Context.Information("Successfully built '" + solutionFilePath.ToString() + "'");
    });
}

void VerifyAppsBuild(string type, string platformIdentifier, string projectPath, string[] buildTypes, Action<string> verificatonMethod, string extraArgs = "")
{
    var outputDirectory = GetBuildFolder(type, projectPath);
    var methodPrefix = "Build" + type + ".Build" + type + "Scene";
    foreach (var buildType in buildTypes)
    {
        // Remove all existing builds and create new build.
        Statics.Context.CleanDirectory(outputDirectory);
        ExecuteUnityMethod(methodPrefix + buildType + " " + extraArgs, platformIdentifier);
        verificatonMethod(outputDirectory);

        // Remove all remaining builds.
        Statics.Context.CleanDirectory(outputDirectory);
    }

    // Remove all remaining builds.
    Statics.Context.CleanDirectory(outputDirectory);
}

Task("PublishPackagesToStorage").Does(() =>
{
    // The environment variables below must be set for this task to succeed
    var apiKey = Argument("AzureStorageAccessKey", EnvironmentVariable("AZURE_STORAGE_ACCESS_KEY"));
    var accountName = EnvironmentVariable("AZURE_STORAGE_ACCOUNT");
    var corePackageVersion = XmlPeek(File("UnityPackageSpecs/AppCenter.unitypackagespec"), "package/@version");
    var zippedPackages = "AppCenter-SDK-Unity-" + corePackageVersion + ".zip";
    var files = GetFiles("output/*.unitypackage");
    Zip("./", zippedPackages, files);
    Information("Publishing packages to blob storage: " + zippedPackages);
    AzureStorage.UploadFileToBlob(new AzureStorageSettings
    {
        AccountName = accountName,
        ContainerName = "sdk",
        BlobName = zippedPackages,
        Key = apiKey,
        UseHttps = true
    }, zippedPackages);
    DeleteFiles(zippedPackages);
    foreach (var package in GetBuiltPackages())
    {
        Information("Publishing packages to blob storage: " + package);
        var fileName = Path.GetFileNameWithoutExtension(package); // AppCenterAnalytics-v1.0.0
        var index = fileName.IndexOf('-'); // 18
        var fileNameLatest = fileName.Substring(0, index) + "Latest.unitypackage"; // AppCenterAnalyticsLatest.unitypackage
        AzureStorage.UploadFileToBlob(new AzureStorageSettings
        {
            AccountName = accountName,
            ContainerName = "sdk",
            BlobName = fileNameLatest,
            Key = apiKey,
            UseHttps = true
        }, package);
    }
}).OnError(HandleError);

Task("RegisterUnity").Does(() =>
{
    var serialNumber = Argument<string>("UnitySerialNumber");
    var username = Argument<string>("UnityUsername");
    var password = Argument<string>("UnityPassword");

    // This will produce an error, but that's okay because the project "noproject" is used so that the
    // root isn't opened by unity, which could potentially remove important .meta files.
    ExecuteUnityCommand($"-serial {serialNumber} -username {username} -password {password}", "noproject");
    var found = false;
    if (Statics.Context.IsRunningOnUnix())
    {
        found = GetFiles("/Library/Application Support/Unity/*.ulf").Count > 0;
    }
    else
    {
        var localAppDataUnityPath = Path.Combine(EnvironmentVariable("LOCALAPPDATA"), @"VirtualStore\ProgramData\Unity\*.ulf");
        found = GetFiles(localAppDataUnityPath).Count + GetFiles(Path.Combine(EnvironmentVariable("PROGRAMDATA"), @"Unity\*.ulf")).Count > 0;
    }
    if (!found)
    {
        throw new Exception("Failed to activate license.");
    }
}).OnError(HandleError);

Task("UnregisterUnity").Does(() =>
{
    var username = Argument<string>("UnityUsername");
    var password = Argument<string>("UnityPassword");
    var result = ExecuteUnityCommand($"-returnLicense -username {username} -password {password}", null);
    if (result != 0)
    {
        throw new Exception("Something went wrong while returning Unity license.");
    }
}).OnError(HandleError);


// Default Task.
Task("Default").IsDependentOn("PrepareAssets");

// Clean up files/directories.
Task("clean")
    .IsDependentOn("RemoveTemporaries")
    .Does(() =>
{
    DeleteDirectoryIfExists("externals");
    DeleteDirectoryIfExists("output");
    CleanDirectories("./**/bin");
    CleanDirectories("./**/obj");
});

void BuildXcodeProject(string projectPath)
{
    var projectFolder = Path.GetDirectoryName(projectPath);
    var buildOutputFolder = Path.Combine(projectFolder, "build");
    XCodeBuild(new XCodeBuildSettings {
        Project = projectPath,
        Scheme = "Unity-iPhone",
        Configuration = "Release",
        DerivedDataPath = buildOutputFolder
    });
}

Task("UpdateCgManifest").Does(()=>
{
    try
    {
        var manifestFilePath = "cgmanifest.json";
        var content = ParseJsonFromFile(manifestFilePath);
        var registrations = (JArray)content["Registrations"];
        foreach (var registration in registrations.Children())
        {
            HanldeRegistration(registration);
        }

        SerializeJsonToPrettyFile(manifestFilePath, content);
    }
    catch (Exception e)
    {
        Warning($"Can't update 'cgmanifest.json'. Error message: {e.Message}");
    }
});

void HanldeRegistration(JToken registration)
{
    var component = registration["component"];
    if (component == null) 
    {
        Warning("Current registration has no 'component' property.");
        return;
    }

    var typeObject = component["type"];
    if (typeObject == null || typeObject.Value<string>() != "git")
    {
        Warning("Current component has no field 'type' or 'type' is not 'git'.");
        return;
    }

    UpdateCommitHash(component);
}

void UpdateCommitHash(JToken component)
{
    var gitData = component["git"];
    var repoUrl = gitData["repositoryUrl"].Value<string>();
    if (ReposToUpdate.IndexOf(repoUrl) < 0)
    {
        Warning($"Repository url: {repoUrl}. Current component should not be updated.");
        return;
    }

    var releaseTag = GetReleaseTag(repoUrl);
    if (string.IsNullOrEmpty(releaseTag))
    {
        Warning($"Repository url: {repoUrl}. Release tag '{releaseTag}' was not found.");
        return;
    }

    var tagsUrl = repoUrl.Replace(".git", "/tags").Replace("https://github.com", "https://api.github.com/repos");
    var tagsListJson = HttpGet(tagsUrl);
    var tag = JArray.Parse(tagsListJson).Children().FirstOrDefault(t => t["name"].Value<string>() == releaseTag);
    if (tag == null)
    {
        Warning($"Repository url: {repoUrl}. Tag '{tag}' was not found.");
        return;
    }

    gitData["commitHash"] = tag["commit"]["sha"].Value<string>();
}

string GetReleaseTag(string repoUrl)
{
    switch (repoUrl)
    {
        case AndroidRepoUrl:
            return AndroidSdkVersion;
        case AppleRepoUrl:
            return IosSdkVersion;
        case DotNetRepoUrl:
            return UwpSdkVersion;
        default:
            return null;
    }
}

RunTarget(Target);
