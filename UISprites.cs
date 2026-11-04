using UnityEngine;

namespace UltimateDrive.UI
{
    /// <summary>Sprites generated in code (no image files, no licences to track).</summary>
    public static class UISprites
    {
        static Sprite circle, roundRect, wheel, white;

        public static Sprite White
        {
            get
            {
                if (white == null)
                {
                    var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                    var px = new Color32[16];
                    for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                    t.SetPixels32(px); t.Apply();
                    white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                }
                return white;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeCircle(128);
                return circle;
            }
        }

        /// <summary>9-slice rounded rectangle; use Image.type = Sliced.</summary>
        public static Sprite RoundRect
        {
            get
            {
                if (roundRect == null)
                {
                    const int s = 64; const float r = 18f;
                    var t = NewTex(s);
                    var px = new Color32[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float cx = Mathf.Clamp(x + 0.5f, r, s - r), cy = Mathf.Clamp(y + 0.5f, r, s - r);
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                            float a = Mathf.Clamp01(r - d + 0.5f);
                            px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                        }
                    t.SetPixels32(px); t.Apply();
                    roundRect = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                        SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
                }
                return roundRect;
            }
        }

        /// <summary>Steering wheel: ring, three spokes, hub and an orange top marker.</summary>
        public static Sprite Wheel
        {
            get
            {
                if (wheel == null)
                {
                    const int s = 256; float c = s * 0.5f;
                    var t = NewTex(s);
                    var px = new Color32[s * s];
                    for (int y = 0; y < s; y++)
                        for (int x = 0; x < s; x++)
                        {
                            float dx = x + 0.5f - c, dy = y + 0.5f - c;
                            float d = Mathf.Sqrt(dx * dx + dy * dy);
                            float a = 0f; Color col = Color.white;
                            float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 108f) / 13f + 0.5f);
                            a = Mathf.Max(a, Mathf.Clamp01(ring));
                            if (d < 100f)
                            {
                                bool spokeH = Mathf.Abs(dy) < 8f && (dy < 8f);               // left + right
                                bool spokeD = Mathf.Abs(dx) < 8f && dy < 0f;                 // bottom
                                if (spokeH || spokeD) a = 1f;
                                if (d < 26f) a = 1f;                                         // hub
                            }
                            if (Mathf.Abs(dx) < 7f && dy > 94f && dy < 122f) { a = 1f; col = new Color(1f, 0.45f, 0.08f); }
                            px[y * s + x] = new Color32((byte)(col.r * 255), (byte)(col.g * 255), (byte)(col.b * 255), (byte)(Mathf.Clamp01(a) * 255f));
                        }
                    t.SetPixels32(px); t.Apply();
                    wheel = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
                }
                return wheel;
            }
        }

        static Sprite MakeCircle(int s)
        {
            var t = NewTex(s);
            var px = new Color32[s * s];
            float c = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(c, c));
                    float a = Mathf.Clamp01(c - d);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
        }

        static Texture2D NewTex(int s)
        {
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }
    }
}
