using UnityEngine;
using UltimateDrive.Core;
using UltimateDrive.Audio;

namespace UltimateDrive.Vehicle
{
    /// <summary>
    /// Builds a sports car from primitives (no art files, no licensing). Root origin is at ground level, +Z is forward.
    /// BuildShowcase makes a display-only car (menu); Build makes the drivable car with 4 WheelColliders.
    /// </summary>
    public static class ProceduralCar
    {
        public class Visual
        {
            public Transform[] wheelPivots = new Transform[4];   // FL, FR, RL, RR
            public Material paint, tail;
        }

        static Quaternion Q(float x, float y = 0f, float z = 0f) { return Quaternion.Euler(x, y, z); }

        public static Visual BuildBody(Transform root, CarSettings s)
        {
            var v = new Visual();
            var cube = PrimitiveMeshes.Cube;
            v.paint = MaterialFactory.Lit(s.bodyColor, 0.65f, 0.88f);
            var glass = MaterialFactory.Lit(new Color(0.03f, 0.05f, 0.08f), 0.4f, 0.95f);
            var black = MaterialFactory.Lit(new Color(0.02f, 0.02f, 0.025f), 0f, 0.25f);
            var head = MaterialFactory.Lit(new Color(1f, 0.96f, 0.82f), 0f, 0.6f);
            v.tail = MaterialFactory.Lit(new Color(0.55f, 0.03f, 0.03f), 0f, 0.5f);
            var rim = MaterialFactory.Lit(new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            var tyre = MaterialFactory.Lit(new Color(0.03f, 0.03f, 0.03f), 0f, 0.1f);

            float hw = 0.92f;
            PrimitiveMeshes.Part(root, "Chassis", cube, new Vector3(0, 0.52f, 0), new Vector3(hw * 2, 0.42f, 4.4f), Quaternion.identity, v.paint);
            PrimitiveMeshes.Part(root, "Hood", cube, new Vector3(0, 0.77f, 1.28f), new Vector3(1.74f, 0.12f, 1.5f), Q(6f), v.paint);
            PrimitiveMeshes.Part(root, "RearDeck", cube, new Vector3(0, 0.78f, -1.55f), new Vector3(1.74f, 0.12f, 1.05f), Q(-4f), v.paint);
            PrimitiveMeshes.Part(root, "Cabin", cube, new Vector3(0, 0.98f, -0.25f), new Vector3(1.5f, 0.5f, 1.9f), Quaternion.identity, glass);
            PrimitiveMeshes.Part(root, "Roof", cube, new Vector3(0, 1.25f, -0.3f), new Vector3(1.4f, 0.06f, 1.35f), Quaternion.identity, v.paint);
            PrimitiveMeshes.Part(root, "Windscreen", cube, new Vector3(0, 1.03f, 0.72f), new Vector3(1.46f, 0.05f, 0.75f), Q(-38f), glass);
            PrimitiveMeshes.Part(root, "RearScreen", cube, new Vector3(0, 1.05f, -1.22f), new Vector3(1.42f, 0.05f, 0.7f), Q(34f), glass);
            PrimitiveMeshes.Part(root, "Splitter", cube, new Vector3(0, 0.32f, 2.22f), new Vector3(1.8f, 0.05f, 0.36f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "Diffuser", cube, new Vector3(0, 0.33f, -2.2f), new Vector3(1.7f, 0.06f, 0.3f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "SkirtL", cube, new Vector3(-0.94f, 0.34f, 0f), new Vector3(0.06f, 0.1f, 2.5f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "SkirtR", cube, new Vector3(0.94f, 0.34f, 0f), new Vector3(0.06f, 0.1f, 2.5f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "GrilleBar", cube, new Vector3(0, 0.46f, 2.21f), new Vector3(0.9f, 0.14f, 0.04f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "HeadL", cube, new Vector3(-0.62f, 0.63f, 2.2f), new Vector3(0.42f, 0.1f, 0.05f), Quaternion.identity, head);
            PrimitiveMeshes.Part(root, "HeadR", cube, new Vector3(0.62f, 0.63f, 2.2f), new Vector3(0.42f, 0.1f, 0.05f), Quaternion.identity, head);
            PrimitiveMeshes.Part(root, "TailL", cube, new Vector3(-0.6f, 0.68f, -2.2f), new Vector3(0.5f, 0.1f, 0.05f), Quaternion.identity, v.tail);
            PrimitiveMeshes.Part(root, "TailR", cube, new Vector3(0.6f, 0.68f, -2.2f), new Vector3(0.5f, 0.1f, 0.05f), Quaternion.identity, v.tail);
            PrimitiveMeshes.Part(root, "MirrorL", cube, new Vector3(-0.9f, 0.98f, 0.42f), new Vector3(0.18f, 0.1f, 0.12f), Quaternion.identity, black);
            PrimitiveMeshes.Part(root, "MirrorR", cube, new Vector3(0.9f, 0.98f, 0.42f), new Vector3(0.18f, 0.1f, 0.12f), Quaternion.identity, black);
            if (s.spoiler)
            {
                PrimitiveMeshes.Part(root, "Wing", cube, new Vector3(0, 1.12f, -2.05f), new Vector3(1.7f, 0.05f, 0.42f), Q(-6f), black);
                PrimitiveMeshes.Part(root, "WingStandL", cube, new Vector3(-0.6f, 0.95f, -1.98f), new Vector3(0.05f, 0.3f, 0.2f), Quaternion.identity, black);
                PrimitiveMeshes.Part(root, "WingStandR", cube, new Vector3(0.6f, 0.95f, -1.98f), new Vector3(0.05f, 0.3f, 0.2f), Quaternion.identity, black);
            }

            // wheels: pivot (gets the WheelCollider pose) > tyre + rim cylinders turned onto the axle
            float zf = s.wheelBase * 0.5f, x = s.trackWidth * 0.5f;
            Vector3[] pos = { new Vector3(-x, s.wheelRadius, zf), new Vector3(x, s.wheelRadius, zf), new Vector3(-x, s.wheelRadius, -zf), new Vector3(x, s.wheelRadius, -zf) };
            string[] names = { "Wheel_FL", "Wheel_FR", "Wheel_RL", "Wheel_RR" };
            float d = s.wheelRadius * 2f;
            for (int i = 0; i < 4; i++)
            {
                var pivot = new GameObject(names[i]).transform;
                pivot.SetParent(root, false);
                pivot.localPosition = pos[i];
                PrimitiveMeshes.Part(pivot, "Tyre", PrimitiveMeshes.Cylinder, Vector3.zero, new Vector3(d, s.wheelWidth * 0.5f, d), Q(0, 0, 90f), tyre);
                PrimitiveMeshes.Part(pivot, "Rim", PrimitiveMeshes.Cylinder, Vector3.zero, new Vector3(d * 0.64f, s.wheelWidth * 0.51f, d * 0.64f), Q(0, 0, 90f), rim);
                PrimitiveMeshes.Part(pivot, "Spokes", cube, Vector3.zero, new Vector3(s.wheelWidth * 0.52f, d * 0.6f, d * 0.08f), Quaternion.identity, black);
                PrimitiveMeshes.Part(pivot, "Spokes2", cube, Vector3.zero, new Vector3(s.wheelWidth * 0.52f, d * 0.08f, d * 0.6f), Quaternion.identity, black);
                v.wheelPivots[i] = pivot;
            }
            return v;
        }

        /// <summary>Display-only car for menus (no physics).</summary>
        public static GameObject BuildShowcase(CarSettings s, Transform parent)
        {
            var root = new GameObject("Showcase_" + s.carId);
            root.transform.SetParent(parent, false);
            BuildBody(root.transform, s);
            return root;
        }

        /// <summary>Drivable car: Rigidbody, body colliders, 4 WheelColliders, controller, input, effects, audio.</summary>
        public static VehicleController Build(CarSettings s, Vector3 position, Quaternion rotation, bool player)
        {
            var root = new GameObject("Car_" + s.carId);
            root.SetActive(false);                       // configure everything before Awake/OnEnable run
            root.transform.SetPositionAndRotation(position, rotation);
            root.tag = player ? "Player" : "Untagged";

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = s.mass;

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.62f, 0); box.size = new Vector3(1.84f, 0.56f, 4.4f);
            var cabin = root.AddComponent<BoxCollider>();
            cabin.center = new Vector3(0, 1.02f, -0.25f); cabin.size = new Vector3(1.5f, 0.5f, 1.9f);
            // slippery body so scraping a wall slides instead of grabbing
            var pm = new PhysicsMaterial("CarBody");   // Unity 6 name (was PhysicMaterial)
            pm.dynamicFriction = 0.15f; pm.staticFriction = 0.2f; pm.bounciness = 0.05f;
            pm.frictionCombine = PhysicsMaterialCombine.Minimum;
            box.sharedMaterial = pm; cabin.sharedMaterial = pm;

            var vis = BuildBody(root.transform, s);

            var wheels = new VehicleController.Wheel[4];
            string[] names = { "WC_FL", "WC_FR", "WC_RL", "WC_RR" };
            float zf = s.wheelBase * 0.5f, x = s.trackWidth * 0.5f;
            Vector2[] xz = { new Vector2(-x, zf), new Vector2(x, zf), new Vector2(-x, -zf), new Vector2(x, -zf) };
            for (int i = 0; i < 4; i++)
            {
                var wcGo = new GameObject(names[i]);
                wcGo.transform.SetParent(root.transform, false);
                // collider origin is the top of the suspension; the wheel hangs half the travel below it at rest
                wcGo.transform.localPosition = new Vector3(xz[i].x, s.wheelRadius + s.suspensionDistance * 0.5f, xz[i].y);
                var wc = wcGo.AddComponent<WheelCollider>();
                bool front = i < 2;
                bool powered = s.drivetrain == Drivetrain.AWD || (s.drivetrain == Drivetrain.FWD && front) || (s.drivetrain == Drivetrain.RWD && !front);
                wheels[i] = new VehicleController.Wheel { collider = wc, visual = vis.wheelPivots[i], steer = front, powered = powered, handbrake = !front };
            }

            var vc = root.AddComponent<VehicleController>();
            if (player) root.AddComponent<PlayerVehicleInput>();

            // activate first: AudioSource.Play and ParticleSystem.Play do nothing on inactive objects
            root.SetActive(true);
            vc.Initialize(s, wheels);
            root.AddComponent<VehicleEffects>().Init(vc, vis.tail);
            root.AddComponent<VehicleAudio>().Init(vc, player);
            return vc;
        }
    }
}
