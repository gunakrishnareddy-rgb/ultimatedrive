using UnityEngine;
using UltimateDrive.Core;

namespace UltimateDrive.World
{
    /// <summary>Generates the prototype circuit: a closed path, road and kerb meshes with generated textures.</summary>
    public static class TrackBuilder
    {
        public const float RoadHalfWidth = 8f;

        /// <summary>Closed loop around the origin, roughly 1.6 km long, with a few different corner radii.</summary>
        public static Vector3[] BuildPath(int count)
        {
            var pts = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                float r = 1f + 0.14f * Mathf.Cos(3f * t + 0.6f) + 0.07f * Mathf.Sin(2f * t);
                pts[i] = new Vector3(300f * r * Mathf.Cos(t), 0f, 190f * r * Mathf.Sin(t));
            }
            return pts;
        }

        public static Vector3 Tangent(Vector3[] p, int i)
        {
            int n = p.Length;
            Vector3 t = p[(i + 1) % n] - p[(i - 1 + n) % n];
            t.y = 0f;
            return t.normalized;
        }

        /// <summary>
        /// Flat strip along the path. lateral = centre offset (to the right of travel), halfWidth = strip half width.
        /// UV u runs across (0..1), v runs along (one repeat per uvLength metres).
        /// </summary>
        public static GameObject BuildStrip(string name, Transform parent, Vector3[] path, float lateral, float halfWidth,
            float y, Material mat, float uvLength, bool collider)
        {
            int n = path.Length;
            var verts = new Vector3[(n + 1) * 2];
            var uvs = new Vector2[verts.Length];
            var tris = new int[n * 6];
            float dist = 0f;
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                Vector3 t = Tangent(path, k);
                Vector3 right = Vector3.Cross(Vector3.up, t);
                Vector3 p = path[k];
                verts[i * 2] = p + right * (lateral - halfWidth) + Vector3.up * y;
                verts[i * 2 + 1] = p + right * (lateral + halfWidth) + Vector3.up * y;
                if (i > 0) dist += Vector3.Distance(path[k], path[(k - 1 + n) % n]);
                uvs[i * 2] = new Vector2(0f, dist / uvLength);
                uvs[i * 2 + 1] = new Vector2(1f, dist / uvLength);
            }
            for (int i = 0; i < n; i++)
            {
                int l0 = i * 2, r0 = i * 2 + 1, l1 = (i + 1) * 2, r1 = (i + 1) * 2 + 1, ti = i * 6;
                tris[ti] = l0; tris[ti + 1] = l1; tris[ti + 2] = r0;
                tris[ti + 3] = r0; tris[ti + 4] = l1; tris[ti + 5] = r1;
            }
            var mesh = new Mesh { name = name };
            mesh.vertices = verts; mesh.uv = uvs; mesh.triangles = tris;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go;
        }

        static Texture2D NewTex(int w, int h, bool mips)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mips);
            t.wrapModeU = TextureWrapMode.Clamp;
            t.wrapModeV = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 8;
            return t;
        }

        /// <summary>Asphalt with worn wheel lines, solid edge lines and a dashed centre line (v repeat = 16 m).</summary>
        public static Material RoadMaterial()
        {
            const int w = 128, h = 256;
            var t = NewTex(w, h, true);
            var px = new Color32[w * h];
            var rng = new System.Random(11);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + 0.5f) / w, v = (y + 0.5f) / h;
                    float g = 0.17f + ((float)rng.NextDouble() - 0.5f) * 0.05f;
                    float wear = Mathf.Exp(-Mathf.Pow((u - 0.3f) / 0.07f, 2f)) + Mathf.Exp(-Mathf.Pow((u - 0.7f) / 0.07f, 2f));
                    g -= wear * 0.035f;
                    Color c = new Color(g, g, g * 1.03f, 1f);
                    bool edge = u < 0.035f || u > 0.965f;
                    bool dash = Mathf.Abs(u - 0.5f) < 0.012f && v < 0.5f;
                    if (edge || dash) c = new Color(0.86f, 0.86f, 0.82f, 1f);
                    px[y * w + x] = c;
                }
            t.SetPixels32(px); t.Apply(true);
            return MaterialFactory.Textured(t, 0.18f);
        }

        /// <summary>Red and white kerb stripes (v repeat = 4 m).</summary>
        public static Material KerbMaterial()
        {
            const int w = 8, h = 64;
            var t = NewTex(w, h, true);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = (y < h / 2) ? new Color32(205, 25, 30, 255) : new Color32(235, 235, 235, 255);
            t.SetPixels32(px); t.Apply(true);
            return MaterialFactory.Textured(t, 0.3f);
        }

        public static Material GrassMaterial(float tiles)
        {
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Repeat; t.anisoLevel = 4;
            var px = new Color32[s * s];
            var rng = new System.Random(5);
            for (int i = 0; i < px.Length; i++)
            {
                float n = (float)rng.NextDouble();
                px[i] = new Color(0.16f + n * 0.07f, 0.34f + n * 0.12f, 0.12f + n * 0.05f, 1f);
            }
            t.SetPixels32(px); t.Apply(true);
            var m = MaterialFactory.Textured(t, 0.05f);
            m.mainTextureScale = new Vector2(tiles, tiles);
            if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", new Vector2(tiles, tiles));
            return m;
        }
    }
}
