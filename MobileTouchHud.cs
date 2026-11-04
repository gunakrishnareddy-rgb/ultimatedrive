using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UltimateDrive.UI;
using UltimateDrive.Core;

namespace UltimateDrive.InputSystemLayer
{
    /// <summary>Press-and-hold UI button. Releases when the finger lifts or slides off.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Pressed { get; private set; }
        public Image target;
        Color idle, down;

        public void Init(Image img)
        {
            target = img; idle = img.color; down = new Color(idle.r + 0.25f, idle.g + 0.25f, idle.b + 0.25f, Mathf.Min(1f, idle.a + 0.25f));
        }

        public void OnPointerDown(PointerEventData e) { Pressed = true; if (target != null) target.color = down; }
        public void OnPointerUp(PointerEventData e) { Release(); }
        public void OnPointerExit(PointerEventData e) { Release(); }
        void Release() { Pressed = false; if (target != null) target.color = idle; }
        void OnDisable() { Pressed = false; }
    }

    /// <summary>On-screen steering wheel. Drag around its centre; rotates up to +/-270 degrees and self-centres on release.</summary>
    public class VirtualSteeringWheel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform wheel;
        public float maxAngle = 270f;
        public float sensitivity = 1.25f;
        public float returnSpeed = 700f;     // degrees per second
        public float Value { get; private set; }

        RectTransform rt;
        float angle;           // current wheel angle, positive = clockwise (steer right)
        float lastPointerAngle;
        bool held;

        void Awake() { rt = (RectTransform)transform; }

        float PointerAngle(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out local);
            // clockwise-positive angle from 12 o'clock
            return Mathf.Atan2(local.x, local.y) * Mathf.Rad2Deg;
        }

        public void OnPointerDown(PointerEventData e) { held = true; lastPointerAngle = PointerAngle(e); }

        public void OnDrag(PointerEventData e)
        {
            float a = PointerAngle(e);
            angle += Mathf.DeltaAngle(lastPointerAngle, a) * sensitivity;
            angle = Mathf.Clamp(angle, -maxAngle, maxAngle);
            lastPointerAngle = a;
        }

        public void OnPointerUp(PointerEventData e) { held = false; }

        void Update()
        {
            if (!held) angle = Mathf.MoveTowards(angle, 0f, returnSpeed * Time.unscaledDeltaTime);
            Value = Mathf.Clamp(angle / (maxAngle * 0.6f), -1f, 1f);   // full lock at 60% of the wheel travel
            if (wheel != null) wheel.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        void OnDisable() { held = false; angle = 0f; Value = 0f; }
    }

    /// <summary>
    /// Android touch HUD, built entirely in code: steering wheel (left), gas and brake (right),
    /// handbrake, nitro, camera, reset and pause. Writes DriveInputState.
    /// </summary>
    public class MobileTouchHud : MonoBehaviour
    {
        public float hudScale = 1f;
        public float hudOpacity = 0.85f;

        Canvas canvas;
        VirtualSteeringWheel wheel;
        HoldButton gas, brake, hand, nitro;
        float thr, brk;

        public bool Visible { get { return canvas != null && canvas.enabled; } }

        void Awake()
        {
            UIBuilder.EnsureEventSystem();
            canvas = UIBuilder.CreateCanvas("MobileHUD", 20, transform);
            var root = UIBuilder.Stretch(canvas.transform, "SafeArea");
            var fit = root.gameObject.AddComponent<SafeAreaFit>();

            var blue = new Color(GameBranding.Blue.r, GameBranding.Blue.g, GameBranding.Blue.b, hudOpacity * 0.55f);
            var orange = new Color(GameBranding.Orange.r, GameBranding.Orange.g, GameBranding.Orange.b, hudOpacity * 0.65f);
            var grey = new Color(0.15f, 0.17f, 0.2f, hudOpacity * 0.7f);
            var red = new Color(0.85f, 0.12f, 0.12f, hudOpacity * 0.65f);
            var green = new Color(0.1f, 0.7f, 0.25f, hudOpacity * 0.65f);

            // Steering wheel, bottom-left.
            var wheelRt = UIBuilder.Rect(root, "SteeringWheel", Vector2.zero, Vector2.zero, new Vector2(0f, 0f), new Vector2(60, 50), new Vector2(380, 380) * hudScale);
            var hit = wheelRt.gameObject.AddComponent<Image>();   // transparent hit area
            hit.color = new Color(0, 0, 0, 0.001f);
            var wheelImg = UIBuilder.Rect(wheelRt, "Rotor", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var wi = wheelImg.gameObject.AddComponent<Image>();
            wi.sprite = UISprites.Wheel; wi.color = new Color(1, 1, 1, hudOpacity); wi.raycastTarget = false;
            wheel = wheelRt.gameObject.AddComponent<VirtualSteeringWheel>();
            wheel.wheel = wheelImg;

            // Pedals, bottom-right.
            gas = MakeHold(root, "Accelerator", "GAS", new Vector2(1, 0), new Vector2(-70, 60), new Vector2(230, 330), green);
            brake = MakeHold(root, "Brake", "BRAKE", new Vector2(1, 0), new Vector2(-330, 60), new Vector2(210, 240), red);
            hand = MakeHold(root, "Handbrake", "HAND", new Vector2(1, 0), new Vector2(-330, 320), new Vector2(210, 110), grey);
            nitro = MakeHold(root, "Nitro", "NITRO", new Vector2(1, 0), new Vector2(-70, 410), new Vector2(230, 110), orange);

            // Top buttons.
            UIBuilder.MakeButton(root, "CameraButton", "CAM", new Vector2(1, 1), new Vector2(-70, -40), new Vector2(150, 80), grey, DriveInputState.RaiseCamera, true, 28);
            UIBuilder.MakeButton(root, "ResetButton", "RESET", new Vector2(1, 1), new Vector2(-240, -40), new Vector2(170, 80), grey, DriveInputState.RaiseReset, true, 28);
            UIBuilder.MakeButton(root, "PauseButton", "II", new Vector2(1, 1), new Vector2(-430, -40), new Vector2(110, 80), grey, DriveInputState.RaisePause, true, 32);
        }

        HoldButton MakeHold(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var rt = UIBuilder.Rect(parent, name, anchor, anchor, new Vector2(1, 0), pos, size * hudScale);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.RoundRect; img.type = Image.Type.Sliced; img.color = color;
            UIBuilder.Label(rt, "Label", label, 36, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var hb = rt.gameObject.AddComponent<HoldButton>();
            hb.Init(img);
            return hb;
        }

        public void SetVisible(bool v) { if (canvas != null) canvas.enabled = v; }

        void Update()
        {
            if (canvas == null || !canvas.enabled) return;
            float dt = Time.unscaledDeltaTime;
            thr = Mathf.MoveTowards(thr, gas.Pressed ? 1f : 0f, (gas.Pressed ? 5f : 8f) * dt);
            brk = Mathf.MoveTowards(brk, brake.Pressed ? 1f : 0f, (brake.Pressed ? 6f : 10f) * dt);
            DriveInputState.Steer = wheel.Value;
            DriveInputState.Throttle = thr;
            DriveInputState.Brake = brk;
            DriveInputState.Handbrake = hand.Pressed;
            DriveInputState.Nitro = nitro.Pressed;
        }

        void OnDisable() { DriveInputState.Clear(); }
    }

    /// <summary>Keeps a panel inside the screen safe area (notches, gesture bars).</summary>
    public class SafeAreaFit : MonoBehaviour
    {
        Rect last;
        Vector2Int lastSize;

        void Update()
        {
            var sa = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && size == lastSize) return;
            last = sa; lastSize = size;
            var rt = (RectTransform)transform;
            if (size.x <= 0 || size.y <= 0) return;
            rt.anchorMin = new Vector2(sa.xMin / size.x, sa.yMin / size.y);
            rt.anchorMax = new Vector2(sa.xMax / size.x, sa.yMax / size.y);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
    }
}
