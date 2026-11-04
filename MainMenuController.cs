using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UltimateDrive.Audio;
using UltimateDrive.Core;
using UltimateDrive.SaveSystem;
using UltimateDrive.Settings;
using UltimateDrive.Vehicle;

namespace UltimateDrive.UI
{
    /// <summary>
    /// Main menu: a 3D showcase of the selected car on a turntable, the title and tagline, and buttons.
    /// Modes that are not built yet are shown disabled and labelled COMING SOON (no dead buttons).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        Transform cam;
        GameObject showcase;
        int carIndex;
        float angle = 25f;
        Text carName, carStats, coinText;
        GameObject mainPanel, settingsPanel;
        Button gfxBtn, volBtn, fpsBtn;

        void Awake()
        {
            Time.timeScale = 1f; AudioListener.pause = false;
            MasterVolume.Apply();
            UIBuilder.EnsureEventSystem();
            BuildStage();
            BuildUi();
            carIndex = Mathf.Max(0, System.Array.IndexOf(CarSettings.BuiltInIds, ProgressStore.Data.selectedCarId));
            ShowCar();
        }

        // ------------------------------------------------------------------ 3D stage
        void BuildStage()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.16f, 0.18f, 0.26f);

            var camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
            var c = camGo.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color(0.02f, 0.025f, 0.04f);
            c.fieldOfView = 38f; c.nearClipPlane = 0.1f; c.farClipPlane = 100f;
            camGo.AddComponent<AudioListener>();
            cam = camGo.transform;

            var key = new GameObject("Key").AddComponent<Light>();
            key.type = LightType.Directional; key.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            key.intensity = 1.1f; key.color = new Color(1f, 0.95f, 0.9f); key.shadows = LightShadows.Soft;
            AddPoint(new Vector3(-5f, 2.5f, -3f), GameBranding.Blue, 40f);
            AddPoint(new Vector3(5f, 2f, 3f), GameBranding.Orange, 30f);

            var dark = MaterialFactory.Lit(new Color(0.05f, 0.055f, 0.065f), 0.7f, 0.85f);
            PrimitiveMeshes.Part(transform, "Platform", PrimitiveMeshes.Cylinder, new Vector3(0, -0.06f, 0), new Vector3(8f, 0.06f, 8f), Quaternion.identity, dark);
            PrimitiveMeshes.Part(transform, "Ring", PrimitiveMeshes.Cylinder, new Vector3(0, -0.1f, 0), new Vector3(8.4f, 0.05f, 8.4f), Quaternion.identity, MaterialFactory.Unlit(GameBranding.Blue));
        }

        void AddPoint(Vector3 pos, Color col, float range)
        {
            var l = new GameObject("Rim").AddComponent<Light>();
            l.type = LightType.Point; l.transform.position = pos; l.color = col; l.range = range;
            // URP uses inverse-square falloff (needs a much higher value than the built-in pipeline)
            l.intensity = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? 25f : 3f;
        }

        void Update()
        {
            angle += 12f * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 center = new Vector3(0f, 0.75f, 0f);
            Vector3 pos = new Vector3(Mathf.Sin(rad) * 8.2f, 2.0f, Mathf.Cos(rad) * 8.2f);
            Vector3 fwd = (center - pos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            cam.position = pos;
            cam.rotation = Quaternion.LookRotation((center - right * 1.9f) - pos, Vector3.up);   // car sits right of centre, clear of the menu
        }

        // ------------------------------------------------------------------ UI
        void BuildUi()
        {
            var canvas = UIBuilder.CreateCanvas("MenuUI", 0, transform);
            var root = UIBuilder.Stretch(canvas.transform, "SafeArea");
            root.gameObject.AddComponent<InputSystemLayer.SafeAreaFit>();

            mainPanel = UIBuilder.Stretch(root, "Main").gameObject;
            var p = mainPanel.transform;
            var left = new Vector2(0, 0.5f);

            UIBuilder.Label(p, "Title", "ULTIMATE\n<color=#FF7314>DRIVE</color>", 120, Color.white, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -50), new Vector2(900, 280)).supportRichText = true;
            UIBuilder.Label(p, "Tagline", GameBranding.Tagline, 26, new Color(0.7f, 0.85f, 1f), TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(84, -330), new Vector2(1000, 40), FontStyle.Normal);

            var size = new Vector2(540, 72);
            var steel = new Color(0.17f, 0.19f, 0.24f, 0.95f);
            UIBuilder.MakeButton(p, "FreeDrive", "FREE DRIVE  /  LAP TRIAL", left, new Vector2(80, -80), size, GameBranding.Blue, StartDrive, true, 32).GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
            Left(UIBuilder.MakeButton(p, "Career", "CAREER   -   COMING SOON", left, new Vector2(80, -164), size, steel, null, false, 28));
            Left(UIBuilder.MakeButton(p, "Race", "RACE MODES   -   COMING SOON", left, new Vector2(80, -248), size, steel, null, false, 28));
            Left(UIBuilder.MakeButton(p, "Garage", "GARAGE   -   COMING SOON", left, new Vector2(80, -332), size, steel, null, false, 28));
            Left(UIBuilder.MakeButton(p, "Settings", "SETTINGS", left, new Vector2(80, -416), size, steel, OpenSettings, true, 32));
            Left(UIBuilder.MakeButton(p, "Quit", "QUIT", left, new Vector2(80, -500), size, GameBranding.Orange, Quit, true, 32));
            if (PlatformInfo.IsWebGL) p.Find("Quit").gameObject.SetActive(false);   // a browser tab cannot quit itself
            // Free Drive anchor row sits above the others; shift the first one up
            ((RectTransform)p.Find("FreeDrive")).anchoredPosition = new Vector2(80, 0);
            ((RectTransform)p.Find("Career")).anchoredPosition = new Vector2(80, -84);
            ((RectTransform)p.Find("Race")).anchoredPosition = new Vector2(80, -168);
            ((RectTransform)p.Find("Garage")).anchoredPosition = new Vector2(80, -252);
            ((RectTransform)p.Find("Settings")).anchoredPosition = new Vector2(80, -336);
            ((RectTransform)p.Find("Quit")).anchoredPosition = new Vector2(80, -420);

            // car chooser (bottom centre-right, under the showcase)
            var bc = new Vector2(0.68f, 0f);
            UIBuilder.MakeButton(p, "PrevCar", "<", bc, new Vector2(-330, 120), new Vector2(86, 86), steel, delegate { Step(-1); }, true, 46);
            UIBuilder.MakeButton(p, "NextCar", ">", bc, new Vector2(330, 120), new Vector2(86, 86), steel, delegate { Step(1); }, true, 46);
            carName = UIBuilder.Label(p, "CarName", "", 52, Color.white, TextAnchor.MiddleCenter, bc, bc, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(560, 70));
            carStats = UIBuilder.Label(p, "CarStats", "", 26, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter, bc, bc, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(700, 40), FontStyle.Normal);

            coinText = UIBuilder.Label(p, "Coins", "", 34, new Color(1f, 0.85f, 0.25f), TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(600, 44));
            coinText.text = ProgressStore.Data.coins + " COINS";

            UIBuilder.Label(p, "Offline", "100% OFFLINE   -   no account, no internet", 22, new Color(1, 1, 1, 0.5f), TextAnchor.LowerLeft,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(80, 28), new Vector2(900, 34), FontStyle.Normal);
            UIBuilder.Label(p, "Version", GameBranding.Version, 22, new Color(1, 1, 1, 0.5f), TextAnchor.LowerRight,
                new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 28), new Vector2(500, 34), FontStyle.Normal);

            BuildSettings(root);
        }

        static void Left(Button b) { ((RectTransform)b.transform).pivot = new Vector2(0, 0.5f); }

        void BuildSettings(Transform root)
        {
            settingsPanel = UIBuilder.Stretch(root, "SettingsPanel").gameObject;
            var s = settingsPanel.transform;
            var dim = UIBuilder.Panel(s, "Dim", new Color(0, 0, 0, 0.75f), false); dim.raycastTarget = true;
            var mid = new Vector2(0.5f, 0.5f);
            UIBuilder.Label(s, "Title", "SETTINGS", 80, Color.white, TextAnchor.MiddleCenter, mid, mid, mid, new Vector2(0, 280), new Vector2(900, 100));
            var size = new Vector2(640, 78);
            var steel = new Color(0.2f, 0.22f, 0.27f);
            float y = 160;
            gfxBtn = UIBuilder.MakeButton(s, "Gfx", "", mid, new Vector2(0, y), size, steel, delegate { GraphicsSettingsLite.Cycle(); RefreshSettings(); }, true, 30); y -= 95;
            volBtn = UIBuilder.MakeButton(s, "Vol", "", mid, new Vector2(0, y), size, steel, delegate { MasterVolume.Cycle(); RefreshSettings(); }, true, 30); y -= 95;
            if (Application.isMobilePlatform && !PlatformInfo.IsWebGL)
            {
                fpsBtn = UIBuilder.MakeButton(s, "Fps", "", mid, new Vector2(0, y), size, steel, delegate { GraphicsSettingsLite.CycleFps(); RefreshSettings(); }, true, 30); y -= 95;
            }
            UIBuilder.MakeButton(s, "Back", "BACK", mid, new Vector2(0, y - 20), size, GameBranding.Blue, CloseSettings);
            UIBuilder.Label(s, "Note", "Auto picks Low / Medium / Ultra for this device. Gameplay is identical on every preset.", 24,
                new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter, mid, mid, mid, new Vector2(0, -300), new Vector2(1400, 40), FontStyle.Normal);
            settingsPanel.SetActive(false);
        }

        void RefreshSettings()
        {
            var cur = GraphicsSettingsLite.Current;
            UIBuilder.SetLabel(gfxBtn, "GRAPHICS: " + cur.ToString().ToUpperInvariant() + (cur == GraphicsPreset.Auto ? " (" + GraphicsSettingsLite.Effective.ToString().ToUpperInvariant() + ")" : ""));
            UIBuilder.SetLabel(volBtn, "VOLUME: " + MasterVolume.Label());
            if (fpsBtn != null) UIBuilder.SetLabel(fpsBtn, "FRAME CAP: " + ProgressStore.Data.targetFps + " FPS");
        }

        void OpenSettings() { mainPanel.SetActive(false); settingsPanel.SetActive(true); RefreshSettings(); }
        void CloseSettings() { settingsPanel.SetActive(false); mainPanel.SetActive(true); ProgressStore.Save(); }

        // ------------------------------------------------------------------ car chooser
        void Step(int d)
        {
            int n = CarSettings.BuiltInIds.Length;
            carIndex = (carIndex + d + n) % n;
            ShowCar();
        }

        void ShowCar()
        {
            if (showcase != null) Destroy(showcase);
            var s = CarSettings.Create(CarSettings.BuiltInIds[carIndex]);
            showcase = ProceduralCar.BuildShowcase(s, transform);
            carName.text = s.displayName;
            carStats.text = s.drivetrain + "   |   " + s.peakTorque.ToString("0") + " Nm   |   " + s.mass.ToString("0") + " kg";
            ProgressStore.Data.selectedCarId = s.carId;
            ProgressStore.MarkDirty();
        }

        void StartDrive()
        {
            ProgressStore.Save();
            SceneManager.LoadScene(GameBranding.DriveScene);
        }

        void Quit()
        {
            ProgressStore.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
