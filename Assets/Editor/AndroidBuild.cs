using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BurgerShop.Editor
{
    public static class AndroidBuild
    {
        public const string PackageId = "com.ic3ma0.burgershop";

        [MenuItem("Burger Shop/Build Android Test APK")]
        public static void BuildApk()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android build target could not be selected.");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            AssetDatabase.SaveAssets();
            string path = Path.GetFullPath("Builds/Android/BurgerShop-0.1.0-arm64.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                target = BuildTarget.Android,
                locationPathName = path,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/android-build-summary.json", JsonUtility.ToJson(new Summary
            {
                result = report.summary.result.ToString(), apk = path,
                bytes = (long)report.summary.totalSize, seconds = report.summary.totalTime.TotalSeconds,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings,
                unity = Application.unityVersion, packageId = PackageId
            }, true));
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android APK build failed. See Logs/android-build-summary.json.");
            Debug.Log("BURGER_SHOP_ANDROID_APK_OK " + path);
        }

        [Serializable]
        sealed class Summary
        {
            public string result, apk, unity, packageId;
            public long bytes;
            public double seconds;
            public int errors, warnings;
        }
    }
}
