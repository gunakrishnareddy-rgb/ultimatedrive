using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UltimateDrive.Audio;
using UltimateDrive.Core;
using UltimateDrive.InputSystemLayer;
using UltimateDrive.SaveSystem;
using UltimateDrive.Settings;

namespace UltimateDrive.UI
{
    /// <summary>Esc / Start / Android Back / HUD button opens it. Every button here does something.</summary>
    public class PauseMenu : MonoBehaviour
    {
        Canvas canvas;
        Button graphicsBtn, fpsBtn, volumeBtn;
        bool open;

        void Awake()
        {
            UIBuilder.EnsureEventSystem();
            canvas = UIBuilder.CreateCanvas("PauseMenu", 50, transform);
            var dim = UIBuilder.Panel(canvas.transform, "Dim", new Color(0, 0, 0, 0.6f), false);
            dim.raycastTarget = true;

            UIBuilder.Label(canvas.transform, "Title", "PAUSED", 90, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(900, 120));

            var mid = new Vector2(0.5f, 0.5f);
            var size = new Vector2(620, 78);
            UIBuilder.MakeButton(canvas.transform, "Resume", "RESUME", mid, new Vector2(0, 220), size, GameBranding.Blue, Toggle);
            UIBuilder.MakeButton(canvas.transform, "Restart", "RESTART", mid, new Vector2(0, 125), size, new Color(0.2f, 0.22f, 0.27f), Restart);
            graphicsBtn = UIBuilder.MakeButton(canvas.transform, "Graphics", "", mid, new Vector2(0, 30), size, new Color(0.2f, 0.22f, 0.27f), CycleGraphics, true, 30);
            volumeBtn = UIBuilder.MakeButton(canvas.transform, "Volume", "", mid, new Vector2(0, -65), size, new Color(0.2f, 0.22f, 0.27f), CycleVolume, true, 30);
            float y = -160;
            if (Application.isMobilePlatform && !PlatformInfo.IsWebGL)
            {
                fpsBtn = UIBuilder.MakeButton(canvas.transform, "Fps", "", mid, new Vector2(0, y), size, new Color(0.2f, 0.22f, 0.27f), CycleFps, true, 30);
                y -= 95;
            }
            UIBuilder.MakeButton(canvas.transform, "MainMenu", "MAIN MENU", mid, new Vector2(0, y), size, GameBranding.Orange, GoMenu);
            RefreshLabels();
            canvas.enabled = false;
        }

        void OnEnable() { DriveInputState.PausePressed += Toggle; }
        void OnDisable() { DriveInputState.PausePressed -= Toggle; }

        void OnDestroy() { Time.timeScale = 1f; AudioListener.pause = false; }

        void RefreshLabels()
        {
            UIBuilder.SetLabel(graphicsBtn, "GRAPHICS: " + GraphicsSettingsLite.Current.ToString().ToUpperInvariant()
                + (GraphicsSettingsLite.Current == GraphicsPreset.Auto ? " (" + GraphicsSettingsLite.Effective.ToString().ToUpperInvariant() + ")" : ""));
            UIBuilder.SetLabel(volumeBtn, "VOLUME: " + MasterVolume.Label());
            if (fpsBtn != null) UIBuilder.SetLabel(fpsBtn, "FRAME CAP: " + ProgressStore.Data.targetFps + " FPS");
        }

        void Toggle()
        {
            open = !open;
            canvas.enabled = open;
            Time.timeScale = open ? 0f : 1f;
            AudioListener.pause = open;
            if (open) { DriveInputState.Clear(); RefreshLabels(); } else ProgressStore.Save();
        }

        void CycleGraphics() { GraphicsSettingsLite.Cycle(); RefreshLabels(); }
        void CycleVolume() { MasterVolume.Cycle(); RefreshLabels(); }
        void CycleFps() { GraphicsSettingsLite.CycleFps(); RefreshLabels(); }

        void Restart()
        {
            ProgressStore.Save();
            Time.timeScale = 1f; AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void GoMenu()
        {
            ProgressStore.Save();
            Time.timeScale = 1f; AudioListener.pause = false;
            SceneManager.LoadScene(GameBranding.MenuScene);
        }
    }
}
