using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UltimateDrive.Audio;
using UltimateDrive.Core;
using UltimateDrive.Settings;

namespace UltimateDrive.UI
{
    /// <summary>Scene 0. Shows the title and tagline, applies saved settings, then loads the main menu.</summary>
    public class BootLoader : MonoBehaviour
    {
        void Awake()
        {
            Application.runInBackground = false;
            MasterVolume.Apply();
            GraphicsSettingsLite.ApplyFrameRate();

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = GameBranding.Black;
            camGo.AddComponent<AudioListener>();

            var canvas = UIBuilder.CreateCanvas("Splash", 0, transform);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            var mid = new Vector2(0.5f, 0.5f);
            var title = UIBuilder.Label(canvas.transform, "Title", "ULTIMATE <color=#FF7314>DRIVE</color>", 130, Color.white, TextAnchor.MiddleCenter,
                mid, mid, mid, new Vector2(0, 50), new Vector2(1600, 180));
            title.supportRichText = true;
            UIBuilder.Label(canvas.transform, "Tagline", GameBranding.Tagline, 40, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter,
                mid, mid, mid, new Vector2(0, -60), new Vector2(1700, 60), FontStyle.Normal);
            UIBuilder.Label(canvas.transform, "Loading", "LOADING...", 26, new Color(1, 1, 1, 0.5f), TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(600, 40), FontStyle.Normal);
            StartCoroutine(Run(group));
        }

        IEnumerator Run(CanvasGroup group)
        {
            var op = SceneManager.LoadSceneAsync(GameBranding.MenuScene);
            op.allowSceneActivation = false;
            float t = 0f;
            while (t < 2.2f || op.progress < 0.9f)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Clamp01(t / 0.6f);
                yield return null;
            }
            for (float f = 1f; f > 0f; f -= Time.unscaledDeltaTime * 3f) { group.alpha = f; yield return null; }
            op.allowSceneActivation = true;
        }
    }
}
