#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UltimateDrive.Core;
using UltimateDrive.InputSystemLayer;
using UltimateDrive.UI;
using UltimateDrive.World;

namespace UltimateDrive.EditorTools
{
    /// <summary>
    /// Creates the three scenes (Boot, MainMenu, PrototypeDrive), adds them to Build Settings in the right order and applies
    /// project settings (landscape, IL2CPP, ARM64, no INTERNET permission, runtime shaders kept in builds).
    /// Runs once automatically when the scripts are first imported. Menu: UltimateDrive > Setup.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string SceneDir = "Assets/Scenes";

        static ProjectSetup() { EditorApplication.delayCall += AutoSetup; }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (File.Exists(SceneDir + "/Boot.unity")) return;
            if (SessionState.GetBool("ud_setup_ran", false)) return;
            SessionState.SetBool("ud_setup_ran", true);
            CreateAll();
        }

        [MenuItem("UltimateDrive/Setup/Create Scenes and Apply Project Settings")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(SceneDir)) AssetDatabase.CreateFolder("Assets", "Scenes");
            string boot = MakeScene("Boot", typeof(BootLoader));
            string menu = MakeScene("MainMenu", typeof(MainMenuController));
            string drive = MakeScene("PrototypeDrive", typeof(PrototypeDriveBuilder));

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(boot, true),
                new EditorBuildSettingsScene(menu, true),
                new EditorBuildSettingsScene(drive, true),
            };
            ApplyProjectSettings();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(boot);
            Debug.Log("[UltimateDrive] Scenes created and project settings applied. Press Play on the Boot scene (or open PrototypeDrive and press Play to drive straight away).");
        }

        static string MakeScene(string name, System.Type component)
        {
            string path = SceneDir + "/" + name + ".unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject(name + "Bootstrap").AddComponent(component);
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        [MenuItem("UltimateDrive/Setup/Apply Project Settings Only")]
        public static void ApplyProjectSettings()
        {
            if (PlayerSettings.productName == "" || PlayerSettings.productName.StartsWith("My project") || PlayerSettings.productName == "UltimateDrive")
                PlayerSettings.productName = "Ultimate Drive";
            if (PlayerSettings.companyName == "DefaultCompany") PlayerSettings.companyName = "UltimateDrive";
            PlayerSettings.runInBackground = false;

            // Android: landscape, IL2CPP, ARM64, fully offline (no INTERNET permission)
            try
            {
                PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
                PlayerSettings.allowedAutorotateToPortrait = false;
                PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
                PlayerSettings.allowedAutorotateToLandscapeLeft = true;
                PlayerSettings.allowedAutorotateToLandscapeRight = true;
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                PlayerSettings.Android.forceInternetPermission = false;
            }
            catch (System.Exception e) { Debug.LogWarning("[Setup] Android settings skipped: " + e.Message); }

            // WebGL (Vercel): compression OFF so any host works, Vercel compresses on the fly; full-window "Minimal" page
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = false;
                PlayerSettings.WebGL.dataCaching = true;
                PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
                PlayerSettings.WebGL.template = "APPLICATION:Minimal";
            }
            catch (System.Exception e) { Debug.LogWarning("[Setup] WebGL settings skipped: " + e.Message); }

            // Windows
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            // everything is created at runtime with Shader.Find, so the shaders must be forced into builds
            AlwaysInclude("Universal Render Pipeline/Lit", "Universal Render Pipeline/Unlit", "Standard",
                "Skybox/Procedural", "Sprites/Default", "UI/Default");
        }

        static void AlwaysInclude(params string[] shaderNames)
        {
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var n in shaderNames)
            {
                var sh = Shader.Find(n);
                if (sh == null) continue;
                bool have = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == sh) { have = true; break; }
                if (have) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = sh;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- test helpers ----
        const string TouchMenu = "UltimateDrive/Test/Touch Controls In Editor";

        [MenuItem(TouchMenu)]
        static void ToggleTouch() { EditorPrefs.SetBool(PlatformControlManager.EditorTouchKey, !EditorPrefs.GetBool(PlatformControlManager.EditorTouchKey, false)); }

        [MenuItem(TouchMenu, true)]
        static bool ToggleTouchValidate() { Menu.SetChecked(TouchMenu, EditorPrefs.GetBool(PlatformControlManager.EditorTouchKey, false)); return true; }

        [MenuItem("UltimateDrive/Test/Open Save Folder")]
        static void OpenSave() { EditorUtility.RevealInFinder(Application.persistentDataPath); }

        [MenuItem("UltimateDrive/Test/Delete Save Data")]
        static void DeleteSave()
        {
            if (!EditorUtility.DisplayDialog("Delete save", "Delete the Ultimate Drive progress file?", "Delete", "Cancel")) return;
            string dir = Path.Combine(Application.persistentDataPath, "UltimateDrive");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
    }
}
#endif
