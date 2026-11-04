using UnityEngine;
using UnityEngine.UI;
using UltimateDrive.Audio;
using UltimateDrive.CameraSystem;
using UltimateDrive.Core;
using UltimateDrive.InputSystemLayer;
using UltimateDrive.SaveSystem;
using UltimateDrive.Settings;
using UltimateDrive.UI;
using UltimateDrive.Vehicle;

namespace UltimateDrive.World
{
    /// <summary>
    /// Put this on one empty GameObject in the PrototypeDrive scene. At start it builds the whole level in code:
    /// sky and sun, ground, circuit, scenery, start gantry, the player's car, camera, controls, HUD and pause menu.
    /// </summary>
    public class PrototypeDriveBuilder : MonoBehaviour
    {
        const int PathPoints = 360;

        VehicleController player;
        Vector3[] path;

        void Awake()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            MasterVolume.Apply();
            DriveInputState.Clear();

            var preset = GraphicsSettingsLite.Effective;
            BuildEnvironment(preset);
            path = TrackBuilder.BuildPath(PathPoints);
            BuildWorld(preset);
            SpawnPlayer();
            BuildCamera(preset);
            PlatformControlManager.Ensure();
            BuildGameplayUi();
        }

        // ---------------------------------------------------------------- environment
        void BuildEnvironment(GraphicsPreset preset)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(46f, -35f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.25f;
            sun.shadows = preset == GraphicsPreset.Low ? LightShadows.Hard : LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            RenderSettings.sun = sun;

            var skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetFloat("_AtmosphereThickness", 1.05f);
                sky.SetFloat("_Exposure", 1.15f);
                sky.SetColor("_SkyTint", new Color(0.5f, 0.62f, 0.85f));
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = preset == GraphicsPreset.Low ? 0.0011f : 0.0005f;
            RenderSettings.fogColor = new Color(0.72f, 0.8f, 0.9f);
            DynamicGI.UpdateEnvironment();
        }

        // ---------------------------------------------------------------- world
        void BuildWorld(GraphicsPreset preset)
        {
            var root = new GameObject("World").transform;

            // ground: a huge box whose top is at y = 0
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root, false);
            ground.transform.position = new Vector3(0f, -1f, 0f);
            ground.transform.localScale = new Vector3(3000f, 2f, 3000f);
            ground.AddComponent<MeshFilter>().sharedMesh = PrimitiveMeshes.Cube;
            ground.AddComponent<MeshRenderer>().sharedMaterial = TrackBuilder.GrassMaterial(500f);
            ground.AddComponent<BoxCollider>();
            ground.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // invisible walls at the world edge
            string[] wn = { "WallN", "WallS", "WallE", "WallW" };
            Vector3[] wp = { new Vector3(0, 10, 1500), new Vector3(0, 10, -1500), new Vector3(1500, 10, 0), new Vector3(-1500, 10, 0) };
            Vector3[] ws = { new Vector3(3000, 20, 2), new Vector3(3000, 20, 2), new Vector3(2, 20, 3000), new Vector3(2, 20, 3000) };
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject(wn[i]); w.transform.SetParent(root, false);
                w.transform.position = wp[i]; w.AddComponent<BoxCollider>().size = ws[i];
            }

            // road + kerbs
            var roadMat = TrackBuilder.RoadMaterial();
            var kerbMat = TrackBuilder.KerbMaterial();
            float hw = TrackBuilder.RoadHalfWidth;
            TrackBuilder.BuildStrip("Road", root, path, 0f, hw, 0.05f, roadMat, 16f, true);
            TrackBuilder.BuildStrip("KerbL", root, path, -(hw + 0.6f), 0.6f, 0.06f, kerbMat, 4f, false);
            TrackBuilder.BuildStrip("KerbR", root, path, hw + 0.6f, 0.6f, 0.06f, kerbMat, 4f, false);

            BuildScenery(root, preset);
            BuildInfield(root);
            BuildGantry(root);
        }

        float DistanceToPath(Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < path.Length; i += 2)
            {
                float dx = p.x - path[i].x, dz = p.z - path[i].z;
                float d = dx * dx + dz * dz;
                if (d < best) best = d;
            }
            return Mathf.Sqrt(best);
        }

        void BuildScenery(Transform root, GraphicsPreset preset)
        {
            var rng = new System.Random(1234);
            var scenery = new GameObject("Scenery").transform;
            scenery.SetParent(root, false);

            var trunk = MaterialFactory.Lit(new Color(0.28f, 0.18f, 0.1f), 0f, 0.1f);
            var leafA = MaterialFactory.Lit(new Color(0.1f, 0.32f, 0.12f), 0f, 0.1f);
            var leafB = MaterialFactory.Lit(new Color(0.16f, 0.4f, 0.14f), 0f, 0.1f);
            trunk.enableInstancing = true; leafA.enableInstancing = true; leafB.enableInstancing = true;

            int wantTrees = preset == GraphicsPreset.Low ? 90 : (preset == GraphicsPreset.Medium ? 170 : 260);
            int placed = 0;
            for (int attempt = 0; attempt < 3000 && placed < wantTrees; attempt++)
            {
                var p = new Vector3(Rand(rng, -900f, 900f), 0f, Rand(rng, -700f, 700f));
                if (DistanceToPath(p) < 24f) continue;
                float h = Rand(rng, 5f, 10f);
                var t = new GameObject("Tree"); t.transform.SetParent(scenery, false); t.transform.position = p;
                var tr = PrimitiveMeshes.Part(t.transform, "Trunk", PrimitiveMeshes.Cylinder, new Vector3(0, h * 0.5f, 0), new Vector3(0.6f, h * 0.5f, 0.6f), Quaternion.identity, trunk);
                var cap = t.AddComponent<CapsuleCollider>(); cap.center = new Vector3(0, h * 0.5f, 0); cap.height = h; cap.radius = 0.3f;
                float s = Rand(rng, 3.5f, 6f);
                PrimitiveMeshes.Part(t.transform, "Leaves", PrimitiveMeshes.Sphere, new Vector3(0, h + s * 0.25f, 0), new Vector3(s, s * 1.15f, s), Quaternion.identity, (placed % 2 == 0) ? leafA : leafB);
                placed++;
            }

            // distant hills for depth
            var hill = MaterialFactory.Lit(new Color(0.3f, 0.38f, 0.33f), 0f, 0.05f);
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f + Rand(rng, -0.1f, 0.1f);
                float r = Rand(rng, 1150f, 1350f);
                float sx = Rand(rng, 300f, 520f), sy = Rand(rng, 120f, 260f);
                var h = PrimitiveMeshes.Part(scenery, "Hill", PrimitiveMeshes.Sphere, new Vector3(Mathf.Cos(a) * r, -sy * 0.25f, Mathf.Sin(a) * r), new Vector3(sx, sy, sx), Quaternion.identity, hill);
                h.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        void BuildInfield(Transform root)
        {
            var rng = new System.Random(99);
            var infield = new GameObject("Infield").transform;
            infield.SetParent(root, false);

            // city blocks
            var wallA = MaterialFactory.Lit(new Color(0.55f, 0.57f, 0.6f), 0f, 0.3f);
            var wallB = MaterialFactory.Lit(new Color(0.35f, 0.38f, 0.45f), 0.2f, 0.5f);
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector3(Rand(rng, -230f, 230f), 0f, Rand(rng, -120f, 120f));
                if (DistanceToPath(p) < 40f || (Mathf.Abs(p.x) < 70f && Mathf.Abs(p.z) < 70f)) continue;
                float w = Rand(rng, 14f, 30f), d = Rand(rng, 14f, 30f), h = Rand(rng, 10f, 55f);
                var b = PrimitiveMeshes.Part(infield, "Building", PrimitiveMeshes.Cube, new Vector3(p.x, h * 0.5f, p.z), new Vector3(w, h, d), Quaternion.identity, i % 2 == 0 ? wallA : wallB);
                b.AddComponent<BoxCollider>();
            }

            // two jump ramps and a cone slalom in the open plaza
            var rampMat = MaterialFactory.Lit(new Color(0.25f, 0.27f, 0.3f), 0f, 0.3f);
            for (int i = 0; i < 2; i++)
            {
                var r = PrimitiveMeshes.Part(infield, "Ramp", PrimitiveMeshes.Cube, new Vector3(-40f + i * 80f, 1.0f, 0f), new Vector3(10f, 0.6f, 20f), Quaternion.Euler(-10f, 0f, 0f), rampMat);
                r.AddComponent<BoxCollider>();
            }
            var cone = MaterialFactory.Lit(GameBranding.Orange, 0f, 0.4f);
            for (int i = 0; i < 18; i++)
            {
                var c = PrimitiveMeshes.Part(infield, "Cone", PrimitiveMeshes.Cube, new Vector3(-55f + i * 6.5f, 0.35f, 40f + (i % 2 == 0 ? 3f : -3f)), new Vector3(0.45f, 0.7f, 0.45f), Quaternion.identity, cone);
                c.AddComponent<BoxCollider>();
                var rb = c.AddComponent<Rigidbody>(); rb.mass = 4f;
            }
        }

        void BuildGantry(Transform root)
        {
            Vector3 p = path[0];
            Vector3 t = TrackBuilder.Tangent(path, 0);
            Quaternion rot = Quaternion.LookRotation(t, Vector3.up);
            Vector3 right = Vector3.Cross(Vector3.up, t);
            float hw = TrackBuilder.RoadHalfWidth + 2.2f;

            var steel = MaterialFactory.Lit(new Color(0.07f, 0.075f, 0.09f), 0.6f, 0.5f);
            var neonBlue = MaterialFactory.Unlit(GameBranding.Blue);
            var neonOrange = MaterialFactory.Unlit(GameBranding.Orange);

            var g = new GameObject("StartGantry").transform;
            g.SetParent(root, false);
            g.position = p; g.rotation = rot;                       // local +Z = direction of travel, +X = right

            foreach (float side in new[] { -1f, 1f })
            {
                var pillar = PrimitiveMeshes.Part(g, "Pillar", PrimitiveMeshes.Cube, new Vector3(side * hw, 4.5f, 0), new Vector3(0.9f, 9f, 0.9f), Quaternion.identity, steel);
                pillar.AddComponent<BoxCollider>();
                PrimitiveMeshes.Part(g, "PillarGlow", PrimitiveMeshes.Cube, new Vector3(side * hw, 4.5f, -0.48f), new Vector3(0.14f, 8.6f, 0.05f), Quaternion.identity, side < 0 ? neonBlue : neonOrange);
            }
            PrimitiveMeshes.Part(g, "Beam", PrimitiveMeshes.Cube, new Vector3(0, 9.2f, 0), new Vector3(hw * 2 + 0.9f, 3.4f, 0.8f), Quaternion.identity, steel);
            PrimitiveMeshes.Part(g, "GlowTop", PrimitiveMeshes.Cube, new Vector3(0, 11f, -0.2f), new Vector3(hw * 2 + 0.9f, 0.14f, 0.1f), Quaternion.identity, neonBlue);
            PrimitiveMeshes.Part(g, "GlowBottom", PrimitiveMeshes.Cube, new Vector3(0, 7.5f, -0.45f), new Vector3(hw * 2 + 0.9f, 0.12f, 0.1f), Quaternion.identity, neonOrange);

            // world-space canvas: faces the drivers approaching from behind (readable when looking along +Z)
            var cgo = new GameObject("Sign", typeof(RectTransform));
            cgo.transform.SetParent(g, false);
            cgo.transform.localPosition = new Vector3(0, 9.3f, -0.45f);
            cgo.transform.localScale = Vector3.one * 0.012f;
            var rt = (RectTransform)cgo.transform; rt.sizeDelta = new Vector2(1500, 260);
            var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            UIBuilder.Label(cgo.transform, "Title", "ULTIMATE <color=#FF7314>DRIVE</color>", 150, Color.white, TextAnchor.MiddleCenter,
                new Vector2(0, 0.38f), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).supportRichText = true;
            UIBuilder.Label(cgo.transform, "Tag", GameBranding.Tagline, 50, new Color(0.7f, 0.85f, 1f), TextAnchor.MiddleCenter,
                new Vector2(0, 0), new Vector2(1, 0.38f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        }

        static float Rand(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        // ---------------------------------------------------------------- player and camera
        void SpawnPlayer()
        {
            string id = ProgressStore.Data.selectedCarId;
            var settings = CarSettings.Create(id);
            int i = (PathPoints - 8) % PathPoints;
            Vector3 t = TrackBuilder.Tangent(path, i);
            player = ProceduralCar.Build(settings, path[i] + Vector3.up * 0.7f, Quaternion.LookRotation(t, Vector3.up), true);
        }

        void BuildCamera(GraphicsPreset preset)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.clearFlags = RenderSettings.skybox != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.7f, 0.9f);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = preset == GraphicsPreset.Low ? 500f : (preset == GraphicsPreset.Medium ? 900f : 1800f);
            var chase = cam.GetComponent<ChaseCamera>();
            if (chase == null) chase = cam.gameObject.AddComponent<ChaseCamera>();
            chase.SetTarget(player);
        }

        void BuildGameplayUi()
        {
            var lap = gameObject.AddComponent<LapTimer>();
            lap.car = player; lap.path = path;
            var hud = new GameObject("DriveHud").AddComponent<DriveHud>();
            hud.Init(player, lap, !PlatformInfo.IsTouch);
            new GameObject("PauseMenu").AddComponent<PauseMenu>();
        }
    }
}
