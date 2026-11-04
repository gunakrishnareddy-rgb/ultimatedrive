using System.Runtime.InteropServices;
using UnityEngine;

namespace UltimateDrive.Core
{
    /// <summary>Where we are running: touch or not, browser or not. Browsers on phones report isMobilePlatform = false, so WebGL asks the page.</summary>
    public static class PlatformInfo
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int UD_IsCoarsePointer();
        [DllImport("__Internal")] static extern void UD_SyncFiles();
#endif

        public static bool IsWebGL
        {
            get { return Application.platform == RuntimePlatform.WebGLPlayer; }
        }

        /// <summary>True when the player should get the on-screen touch controls.</summary>
        public static bool IsTouch
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return UD_IsCoarsePointer() == 1;
#else
                return Application.isMobilePlatform;
#endif
            }
        }

        /// <summary>Save files live in browser storage on WebGL and must be flushed explicitly.</summary>
        public static void FlushSaves()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            UD_SyncFiles();
#endif
        }
    }
}
