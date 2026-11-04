using UnityEngine;
using UnityEngine.UI;
using UltimateDrive.CameraSystem;
using UltimateDrive.Core;
using UltimateDrive.SaveSystem;
using UltimateDrive.Vehicle;
using UltimateDrive.World;

namespace UltimateDrive.UI
{
    /// <summary>Speed, gear, RPM and nitro bars, lap times, coins, and short on-screen messages.</summary>
    public class DriveHud : MonoBehaviour
    {
        VehicleController car;
        LapTimer lap;
        Text speed, gear, lapText, bestText, coinsText, toast, hint;
        Image rpmFill, nitroFill;
        int lastSpeed = -1, lastGear = -99, lastCoins = -1;
        float toastTimer;

        public void Init(VehicleController vc, LapTimer lt, bool showKeyHints)
        {
            car = vc; lap = lt;
            var canvas = UIBuilder.CreateCanvas("HUD", 10, transform);
            var root = UIBuilder.Stretch(canvas.transform, "SafeArea");
            root.gameObject.AddComponent<InputSystemLayer.SafeAreaFit>();

            // top-left: brand + coins
            UIBuilder.Label(root, "Brand", "ULTIMATE <color=#FF7314>DRIVE</color>", 38, Color.white, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 50)).supportRichText = true;
            coinsText = UIBuilder.Label(root, "Coins", "", 34, new Color(1f, 0.85f, 0.25f), TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -82), new Vector2(500, 44));

            // top-centre: lap info
            lapText = UIBuilder.Label(root, "Lap", "", 44, Color.white, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(900, 56));
            bestText = UIBuilder.Label(root, "Best", "", 30, new Color(0.7f, 0.85f, 1f), TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(900, 40));

            // bottom-centre: speedometer
            var panel = UIBuilder.Rect(root, "Speedo", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(560, 210));
            var bg = panel.gameObject.AddComponent<Image>();
            bg.sprite = UISprites.RoundRect; bg.type = Image.Type.Sliced; bg.color = UIBuilder.Dark(0.55f); bg.raycastTarget = false;
            speed = UIBuilder.Label(panel, "Speed", "0", 110, Color.white, TextAnchor.MiddleRight,
                new Vector2(0, 0.35f), new Vector2(0.62f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 0), Vector2.zero);
            UIBuilder.Label(panel, "Unit", "KM/H", 30, new Color(0.7f, 0.85f, 1f), TextAnchor.LowerLeft,
                new Vector2(0.64f, 0.45f), new Vector2(1, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            gear = UIBuilder.Label(panel, "Gear", "N", 80, GameBranding.Orange, TextAnchor.MiddleLeft,
                new Vector2(0.64f, 0.62f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rpmFill = Bar(panel, "Rpm", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.3f), GameBranding.Blue);
            nitroFill = Bar(panel, "Nitro", new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.13f), GameBranding.Orange);

            toast = UIBuilder.Label(root, "Toast", "", 64, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1400, 120));
            toast.supportRichText = true;

            if (showKeyHints)
            {
                hint = UIBuilder.Label(root, "Hint",
                    "W/S gas + brake/reverse   A/D steer   SPACE handbrake   SHIFT nitro   C camera   R reset   ESC pause",
                    24, new Color(1, 1, 1, 0.65f), TextAnchor.LowerLeft,
                    new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 24), new Vector2(1800, 36), FontStyle.Normal);
            }

            lap.LapCompleted += OnLap;
            ChaseCamera.ViewChanged += OnView;
            Show("GO!", 1.6f);
        }

        Image Bar(Transform parent, string name, Vector2 a, Vector2 b, Color c)
        {
            var back = UIBuilder.Rect(parent, name + "Back", a, b, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var bi = back.gameObject.AddComponent<Image>(); bi.sprite = UISprites.White; bi.color = new Color(1, 1, 1, 0.12f); bi.raycastTarget = false;
            var fill = UIBuilder.Stretch(back, name + "Fill");
            var fi = fill.gameObject.AddComponent<Image>();
            fi.sprite = UISprites.White; fi.type = Image.Type.Filled; fi.fillMethod = Image.FillMethod.Horizontal; fi.fillOrigin = 0;
            fi.color = c; fi.raycastTarget = false;
            return fi;
        }

        void OnDestroy()
        {
            if (lap != null) lap.LapCompleted -= OnLap;
            ChaseCamera.ViewChanged -= OnView;
        }

        void OnLap(float t, bool best, int coins)
        {
            Show("LAP " + lap.Laps + "   " + Fmt(t) + (best ? "\n<color=#FF7314>NEW BEST!</color>" : "") + "\n<size=40>+" + coins + " COINS</size>", 4f);
        }

        void OnView(CameraView v) { Show(v.ToString().ToUpperInvariant() + " CAMERA", 1.2f); }

        void Show(string msg, float seconds) { toast.text = msg; toastTimer = seconds; }

        static string Fmt(float t)
        {
            if (t <= 0f) return "--:--.---";
            int m = (int)(t / 60f); float s = t - m * 60f;
            return m + ":" + s.ToString("00.000");
        }

        void Update()
        {
            if (car == null) return;
            int kmh = Mathf.RoundToInt(car.SpeedKmh);
            if (kmh != lastSpeed) { lastSpeed = kmh; speed.text = kmh.ToString(); }
            int g = car.Gear;
            if (g != lastGear) { lastGear = g; gear.text = g < 0 ? "R" : (kmh < 2 && g == 1 ? "N" : g.ToString()); }

            float r = car.Rpm01;
            rpmFill.fillAmount = r;
            rpmFill.color = Color.Lerp(GameBranding.Blue, new Color(1f, 0.1f, 0.1f), Mathf.InverseLerp(0.8f, 1f, r));
            nitroFill.fillAmount = car.NitroCharge;

            lapText.text = lap.Started ? "LAP " + (lap.Laps + 1) + "   " + Fmt(lap.LapTime) : "CROSS THE START LINE";
            bestText.text = "BEST " + Fmt(lap.BestLap) + "    LAST " + Fmt(lap.LastLap);

            int coins = ProgressStore.Data.coins;
            if (coins != lastCoins) { lastCoins = coins; coinsText.text = coins + " COINS"; }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                float a = Mathf.Clamp01(toastTimer / 0.5f);
                toast.color = new Color(1, 1, 1, a);
                if (toastTimer <= 0f) toast.text = "";
            }
        }
    }
}
