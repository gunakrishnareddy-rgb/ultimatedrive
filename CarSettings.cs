using UnityEngine;

namespace UltimateDrive.Vehicle
{
    public enum Drivetrain { RWD, FWD, AWD }

    /// <summary>All tuning for one car. Create > UltimateDrive > Car Settings, or use the built-in factory cars.</summary>
    [CreateAssetMenu(menuName = "UltimateDrive/Car Settings", fileName = "CarSettings")]
    public class CarSettings : ScriptableObject
    {
        [Header("Identity")]
        public string carId = "apex_gt";
        public string displayName = "APEX GT";

        [Header("Body")]
        public float mass = 1400f;
        public Vector3 centerOfMass = new Vector3(0f, 0.38f, 0.05f);    // relative to the car root, which sits at ground level
        public float dragCoefficient = 0.33f;
        public float frontalArea = 2.0f;
        public float downforce = 1.1f;                                 // N per (m/s)^2

        [Header("Wheels and suspension")]
        public float wheelRadius = 0.34f;
        public float wheelWidth = 0.28f;
        public float wheelBase = 2.65f;
        public float trackWidth = 1.58f;
        public float suspensionDistance = 0.2f;
        public float springRate = 35000f;
        public float damperRate = 4500f;
        public float antiRoll = 6000f;

        [Header("Engine and gearbox")]
        public float peakTorque = 480f;
        public float idleRpm = 900f;
        public float redlineRpm = 7600f;
        public AnimationCurve torqueCurve = DefaultCurve();             // multiplier 0..1 over RPM
        public float[] gearRatios = { 3.5f, 2.35f, 1.7f, 1.32f, 1.05f, 0.86f };
        public float reverseRatio = 3.2f;
        public float finalDrive = 3.9f;
        public float drivetrainEfficiency = 0.88f;
        public Drivetrain drivetrain = Drivetrain.RWD;

        [Header("Steering")]
        public float maxSteerAngle = 32f;
        public float highSpeedSteerFactor = 0.28f;
        public float steerFalloffSpeed = 55f;                           // m/s at which the high speed factor is fully applied
        public float steerResponse = 7f;

        [Header("Brakes")]
        public float totalBrakeTorque = 6500f;                          // all four wheels together, Nm
        public float frontBrakeBias = 0.62f;
        public float handbrakeTorque = 5500f;                           // per rear wheel
        public float engineBrakeTorque = 110f;                          // per driven wheel when coasting

        [Header("Grip")]
        public float forwardStiffness = 1.9f;
        public float sidewaysStiffness = 2.3f;
        public float handbrakeSlideFactor = 0.5f;                       // rear sideways grip multiplier while the handbrake is held

        [Header("Nitro")]
        public float nitroTorqueMultiplier = 1.45f;
        public float nitroDuration = 5f;
        public float nitroRecharge = 14f;

        [Header("Look")]
        public Color bodyColor = new Color(0.08f, 0.38f, 0.95f);
        public bool spoiler = true;

        public static AnimationCurve DefaultCurve()
        {
            return new AnimationCurve(
                new Keyframe(900f, 0.55f), new Keyframe(2500f, 0.8f), new Keyframe(4500f, 1f),
                new Keyframe(6200f, 0.97f), new Keyframe(7600f, 0.78f), new Keyframe(9000f, 0.5f));
        }

        // ---- built-in cars (a mini garage until Part 3's catalog arrives) ----
        public static readonly string[] BuiltInIds = { "apex_gt", "vortex_rs", "stallion_v8" };

        public static CarSettings Create(string id)
        {
            var s = CreateInstance<CarSettings>();
            s.hideFlags = HideFlags.DontSave;
            switch (id)
            {
                case "vortex_rs":
                    s.carId = id; s.displayName = "VORTEX RS (AWD)";
                    s.mass = 1520f; s.peakTorque = 560f; s.redlineRpm = 7200f; s.drivetrain = Drivetrain.AWD;
                    s.forwardStiffness = 2.2f; s.sidewaysStiffness = 2.5f; s.nitroTorqueMultiplier = 1.5f;
                    s.bodyColor = new Color(1f, 0.45f, 0.08f); s.finalDrive = 4.0f;
                    break;
                case "stallion_v8":
                    s.carId = id; s.displayName = "STALLION V8";
                    s.mass = 1680f; s.peakTorque = 640f; s.redlineRpm = 6400f; s.idleRpm = 750f;
                    s.gearRatios = new[] { 3.2f, 2.1f, 1.5f, 1.15f, 0.9f, 0.75f }; s.finalDrive = 3.7f;
                    s.forwardStiffness = 1.7f; s.sidewaysStiffness = 2.0f; s.dragCoefficient = 0.38f;
                    s.bodyColor = new Color(0.8f, 0.05f, 0.07f); s.spoiler = false;
                    break;
                default:
                    s.carId = "apex_gt"; s.displayName = "APEX GT";
                    break;
            }
            return s;
        }
    }
}
