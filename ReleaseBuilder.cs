#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimateDrive.ReleaseTools
{
    /// <summary>
    /// Menu UltimateDrive > Build. One click release builds into the project's Builds/ folder:
    ///   Builds/Android/UltimateDrive.apk
    ///   Builds/Windows/UltimateDrive.exe (+ _Data folder) and Builds/UltimateDrive_Windows.zip
    /// Uses the scenes that are ticked in Build Profiles / Build Settings (Boot must be first).
    /// Always builds a release (non-development) player. Needs the Android and Windows build modules installed in Unity Hub.
    /// </summary>
    public static class ReleaseBuilder
    {
        const string Menu = "UltimateDrive/Build/";
        const string ProductFile = "UltimateDrive";

        [MenuItem(Menu + "Android APK")] public static void BuildAndroidApk() { Build(BuildTarget.Android, false); }
        [MenuItem(Menu + "Android App Bundle (AAB, for Google Play)")] public static void BuildAndroidAab() { Build(BuildTarget.Android, true); }
        [MenuItem(Menu + "Windows 64-bit (folder + zip)")] public static void BuildWindows() { Build(BuildTarget.StandaloneWindows64, false); }

        [MenuItem(Menu + "Both (APK + Windows)")]
        public static void BuildBoth()
        {
            if (Build(BuildTarget.Android, false)) Build(BuildTarget.StandaloneWindows64, false);
        }

        static bool Build(BuildTarget target, bool bundle)
        {
            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("Build", "No scenes are ticked in Build Settings. Add Boot (first), MainMenu, Garage, WorldCore and the chunk scenes.", "OK");
                return false;
            }
            if (!Path.GetFileNameWithoutExtension(scenes[0]).ToLowerInvariant().Contains("boot"))
                Debug.LogWarning("[Build] The first scene is '" + scenes[0] + "', not Boot. The game expects Boot at index 0.");

            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            string outPath;
            BuildTargetGroup group;
            if (target == BuildTarget.Android)
            {
                group = BuildTargetGroup.Android;
                EditorUserBuildSettings.buildAppBundle = bundle;
                if (!PlayerSettings.Android.useCustomKeystore)
                    Debug.LogWarning("[Build] No custom keystore set (Player Settings > Publishing Settings). This build is signed with the debug key and cannot be published.");
                outPath = Path.Combine(root, "Android", ProductFile + (bundle ? ".aab" : ".apk"));
            }
            else
            {
                group = BuildTargetGroup.Standalone;
                outPath = Path.Combine(root, "Windows", ProductFile + ".exe");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.connectProfiler = false;

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary sum = report.summary;
            if (sum.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Build] " + target + " build " + sum.result + ": " + sum.totalErrors + " errors. See the Console above.");
                return false;
            }

            Debug.Log("[Build] " + target + " OK: " + outPath + "  (" + (sum.totalSize / (1024 * 1024)) + " MB, " + sum.totalTime.TotalSeconds.ToString("0") + " s)");
            if (target == BuildTarget.StandaloneWindows64) ZipWindows(root);
            EditorUtility.RevealInFinder(outPath);
            return true;
        }

        static void ZipWindows(string root)
        {
            try
            {
                string dir = Path.Combine(root, "Windows");
                string zip = Path.Combine(root, "UltimateDrive_Windows.zip");
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(dir, zip);
                Debug.Log("[Build] Windows zip: " + zip);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Build] Could not create the zip (" + e.Message + "). Zip the Builds/Windows folder by hand: ship the whole folder, not only the .exe.");
            }
        }
    }
}
#endif
