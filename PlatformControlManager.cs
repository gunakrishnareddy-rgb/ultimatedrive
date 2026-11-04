using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateDrive.InputSystemLayer
{
    /// <summary>
    /// Picks the input source automatically: touch HUD on Android/iOS, keyboard + gamepad on desktop.
    /// A gamepad on a phone hides the touch HUD. In the Editor, menu UltimateDrive > Test > Touch Controls In Editor forces the touch HUD.
    /// </summary>
    public class PlatformControlManager : MonoBehaviour
    {
        public const string EditorTouchKey = "ud_editor_touch";

        DesktopDriveInput desktop;
        MobileTouchHud touch;
        bool useTouchPlatform;

        public static PlatformControlManager Ensure()
        {
            var existing = FindFirstObjectByType<PlatformControlManager>();
            if (existing != null) return existing;
            return new GameObject("PlatformControls").AddComponent<PlatformControlManager>();
        }

        void Awake()
        {
            useTouchPlatform = UltimateDrive.Core.PlatformInfo.IsTouch;
#if UNITY_EDITOR
            if (EditorPrefs.GetBool(EditorTouchKey, false)) useTouchPlatform = true;
#endif
            desktop = gameObject.AddComponent<DesktopDriveInput>();
            if (useTouchPlatform)
            {
                touch = gameObject.AddComponent<MobileTouchHud>();
#if !UNITY_WEBGL
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
#endif
            }
            desktop.enabled = !useTouchPlatform;
        }

        bool GamepadConnected()
        {
#if ENABLE_INPUT_SYSTEM
            return Gamepad.current != null;
#else
            return false;
#endif
        }

        void Update()
        {
            if (!useTouchPlatform || touch == null) return;
            bool pad = GamepadConnected();
            if (pad == touch.Visible)
            {
                touch.SetVisible(!pad);
                touch.enabled = !pad;
                desktop.enabled = pad;
            }
        }
    }
}
