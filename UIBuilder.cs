using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UltimateDrive.Core;

namespace UltimateDrive.UI
{
    /// <summary>Tiny uGUI helpers. Every screen in the prototype is built from these in code, so scenes stay empty.</summary>
    public static class UIBuilder
    {
        static Font font;
        public static Font MainFont
        {
            get
            {
                if (font == null)
                {
                    font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);
                }
                return font;
            }
        }

        public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos; rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var rt = Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true)
        {
            var rt = Stretch(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = rounded ? UISprites.RoundRect : UISprites.White;
            img.type = rounded ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 boxSize, FontStyle style = FontStyle.Bold)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax, pivot, anchoredPos, boxSize);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = MainFont; t.text = text; t.fontSize = size; t.color = color; t.alignment = anchor;
            t.fontStyle = style; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Button with a visible label. If interactable is false it shows the label dimmed (for "COMING SOON").</summary>
        public static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 anchoredPos, Vector2 size,
            Color bg, UnityAction onClick, bool interactable = true, int fontSize = 34)
        {
            var rt = Rect(parent, name, anchor, anchor, anchor, anchoredPos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = UISprites.RoundRect; img.type = Image.Type.Sliced; img.color = bg;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
            btn.colors = colors;
            Label(rt, "Label", label, fontSize, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            if (onClick != null) btn.onClick.AddListener(onClick);
            btn.interactable = interactable;
            return btn;
        }

        public static void SetLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static Color Dark(float a = 0.85f) { return new Color(0.04f, 0.045f, 0.055f, a); }
    }
}
