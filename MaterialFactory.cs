using UnityEngine;

namespace UltimateDrive.Core
{
    /// <summary>Runtime materials with shader fallbacks (URP Lit, then Standard, then Sprites/Default) so nothing renders pink.</summary>
    public static class MaterialFactory
    {
        static Shader lit;

        public static Shader LitShader
        {
            get
            {
                if (lit == null)
                {
                    lit = Shader.Find("Universal Render Pipeline/Lit");
                    if (lit == null) lit = Shader.Find("Standard");
                    if (lit == null) lit = Shader.Find("Sprites/Default");
                }
                return lit;
            }
        }

        public static Material Lit(Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            var m = new Material(LitShader);
            SetColor(m, color);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material Textured(Texture2D tex, float smoothness = 0.2f)
        {
            var m = Lit(Color.white, 0f, smoothness);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static Material Unlit(Color c)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh);
            SetColor(m, c);
            return m;
        }
    }

    /// <summary>Built-in primitive meshes without colliders.</summary>
    public static class PrimitiveMeshes
    {
        static Mesh cube, cylinder, sphere;

        public static Mesh Cube { get { if (cube == null) cube = Grab(PrimitiveType.Cube); return cube; } }
        public static Mesh Cylinder { get { if (cylinder == null) cylinder = Grab(PrimitiveType.Cylinder); return cylinder; } }
        public static Mesh Sphere { get { if (sphere == null) sphere = Grab(PrimitiveType.Sphere); return sphere; } }

        static Mesh Grab(PrimitiveType t)
        {
            var go = GameObject.CreatePrimitive(t);
            var m = go.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(go);
            return m;
        }

        public static GameObject Part(Transform parent, string name, Mesh mesh, Vector3 localPos, Vector3 scale, Quaternion localRot, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }
    }
}
