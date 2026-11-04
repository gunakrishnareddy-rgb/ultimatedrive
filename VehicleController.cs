using System;
using UnityEngine;

namespace UltimateDrive.Vehicle
{
    /// <summary>
    /// Raycast-suspension (WheelCollider) car: torque curve engine, automatic gearbox, Ackermann steering,
    /// ABS, traction control, handbrake slide, anti-roll bars, drag and downforce, nitro, flip recovery.
    /// Inputs come from PlayerVehicleInput (or an AI). Wheel order must be FL, FR, RL, RR.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        [Serializable]
        public class Wheel
        {
            public WheelCollider collider;
            public Transform visual;
            public bool steer;
            public bool powered;
            public bool handbrake;
        }

        public CarSettings settings;
        public Wheel[] wheels;

        [Header("Inputs")]
        public float throttleInput;
        public float brakeInput;
        public float steerInput;
        public bool handbrakeInput;
        public bool nitroInput;

        [Header("Assists")]
        public bool tractionControl = true;
        public bool abs = true;

        public float SpeedKmh { get; private set; }
        public float ForwardSpeed { get; private set; }
        public float EngineRpm { get; private set; }
        public int Gear { get { return reversing ? -1 : gear; } }
        public float Rpm01 { get { return settings == null ? 0f : Mathf.InverseLerp(settings.idleRpm, settings.redlineRpm, EngineRpm); } }
        public float NitroCharge { get; private set; }
        public bool NitroActive { get; private set; }
        public float BrakeAmount { get { return brakeAmount; } }
        public bool Reversing { get { return reversing; } }
        public Rigidbody Body { get; private set; }

        int gear = 1;
        float shiftTimer, currentSteer, driveInput, brakeAmount, handbrakeBlend, flippedTimer;
        bool reversing, ready;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            NitroCharge = 1f;
            if (settings != null && wheels != null && wheels.Length == 4) Setup();
        }

        public void Initialize(CarSettings s, Wheel[] w)
        {
            settings = s; wheels = w;
            if (Body == null) Body = GetComponent<Rigidbody>();
            Setup();
        }

        void Setup()
        {
            var s = settings;
            Body.mass = s.mass;
            Body.centerOfMass = s.centerOfMass;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            Body.linearDamping = 0.02f;
            Body.angularDamping = 0.25f;
            for (int i = 0; i < wheels.Length; i++) ConfigureWheel(wheels[i].collider);
            EngineRpm = s.idleRpm;
            ready = true;
        }

        void ConfigureWheel(WheelCollider c)
        {
            var s = settings;
            c.mass = 22f;
            c.radius = s.wheelRadius;
            c.wheelDampingRate = 0.3f;
            c.suspensionDistance = s.suspensionDistance;
            c.forceAppPointDistance = 0f;
            var sp = c.suspensionSpring;
            sp.spring = s.springRate; sp.damper = s.damperRate; sp.targetPosition = 0.5f;
            c.suspensionSpring = sp;
            SetFriction(c, 1f);
            c.ConfigureVehicleSubsteps(5f, 12, 15);
        }

        void SetFriction(WheelCollider c, float sidewaysScale)
        {
            var f = c.forwardFriction;
            f.extremumSlip = 0.4f; f.extremumValue = 1f; f.asymptoteSlip = 0.8f; f.asymptoteValue = 0.6f;
            f.stiffness = settings.forwardStiffness;
            c.forwardFriction = f;
            var sd = c.sidewaysFriction;
            sd.extremumSlip = 0.25f; sd.extremumValue = 1f; sd.asymptoteSlip = 0.5f; sd.asymptoteValue = 0.75f;
            sd.stiffness = settings.sidewaysStiffness * sidewaysScale;
            c.sidewaysFriction = sd;
        }

        void FixedUpdate()
        {
            if (!ready) return;
            float dt = Time.fixedDeltaTime;
            Vector3 v = Body.linearVelocity;
            float speed = v.magnitude;
            ForwardSpeed = Vector3.Dot(v, transform.forward);
            SpeedKmh = speed * 3.6f;

            UpdatePedals();
            UpdateSteering(dt);
            float totalRatio = UpdateEngineAndGearbox(dt);
            ApplyDriveAndBrakes(totalRatio, dt);
            ApplyAntiRoll(0, 1);
            ApplyAntiRoll(2, 3);
            ApplyAero(v, speed);
            UpdateNitro(dt);
            CheckFlip(dt, speed);
        }

        void LateUpdate()
        {
            if (!ready) return;
            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                if (w.visual == null) continue;
                Vector3 p; Quaternion r;
                w.collider.GetWorldPose(out p, out r);
                w.visual.SetPositionAndRotation(p, r);
            }
        }

        // ---- pedals: S brakes while moving forward, and reverses from a standstill ----
        void UpdatePedals()
        {
            float thr = Mathf.Clamp01(throttleInput), brk = Mathf.Clamp01(brakeInput);
            if (!reversing)
            {
                if (brk > 0.1f && thr < 0.1f && ForwardSpeed < 0.8f) reversing = true;
            }
            else
            {
                if (thr > 0.1f && ForwardSpeed > -0.8f) reversing = false;
                else if (brk < 0.05f && thr < 0.05f && Mathf.Abs(ForwardSpeed) < 0.2f) reversing = false;
            }
            if (reversing) { driveInput = brk; brakeAmount = thr; }
            else { driveInput = thr; brakeAmount = brk; }
        }

        void UpdateSteering(float dt)
        {
            var s = settings;
            float speedFactor = Mathf.Lerp(1f, s.highSpeedSteerFactor, Mathf.Clamp01(Mathf.Abs(ForwardSpeed) / s.steerFalloffSpeed));
            float target = Mathf.Clamp(steerInput, -1f, 1f) * s.maxSteerAngle * speedFactor;
            currentSteer = Mathf.Lerp(currentSteer, target, 1f - Mathf.Exp(-s.steerResponse * dt));

            float a = currentSteer * Mathf.Deg2Rad;
            if (Mathf.Abs(a) < 0.0002f)
            {
                wheels[0].collider.steerAngle = 0f; wheels[1].collider.steerAngle = 0f;
                return;
            }
            // Ackermann: the inside wheel turns more than the outside wheel.
            float t = Mathf.Tan(a), wb = s.wheelBase, h = s.trackWidth * 0.5f;
            wheels[0].collider.steerAngle = Mathf.Atan(wb * t / (wb + h * t)) * Mathf.Rad2Deg;
            wheels[1].collider.steerAngle = Mathf.Atan(wb * t / (wb - h * t)) * Mathf.Rad2Deg;
        }

        // returns gearRatio * finalDrive for the current gear
        float UpdateEngineAndGearbox(float dt)
        {
            var s = settings;
            int n = s.gearRatios.Length;

            float drivenRpm = 0f; int count = 0;
            for (int i = 0; i < wheels.Length; i++)
                if (wheels[i].powered) { drivenRpm += Mathf.Abs(wheels[i].collider.rpm); count++; }
            if (count > 0) drivenRpm /= count;

            float ratio = reversing ? s.reverseRatio : s.gearRatios[Mathf.Clamp(gear - 1, 0, n - 1)];
            float totalRatio = ratio * s.finalDrive;
            float rpmFromWheels = drivenRpm * totalRatio;

            // clutch slip: when crawling, the engine can rev above what the wheels demand
            float launch = Mathf.Lerp(s.idleRpm, s.idleRpm + 2800f, driveInput) * (1f - Mathf.Clamp01(rpmFromWheels / 3500f));
            float targetRpm = Mathf.Max(rpmFromWheels, launch);
            EngineRpm = Mathf.Lerp(EngineRpm, targetRpm, 1f - Mathf.Exp(-16f * dt));
            EngineRpm = Mathf.Clamp(EngineRpm, s.idleRpm, s.redlineRpm + 150f);

            shiftTimer -= dt;
            if (!reversing)
            {
                if (Mathf.Abs(ForwardSpeed) < 1.5f && driveInput < 0.1f) gear = 1;
                if (shiftTimer <= 0f)
                {
                    bool wantUp = EngineRpm > s.redlineRpm * 0.93f || (driveInput < 0.45f && rpmFromWheels > s.redlineRpm * 0.7f);
                    float downAt = driveInput > 0.8f ? 0.5f : 0.38f;
                    if (wantUp && gear < n) { gear++; shiftTimer = 0.28f; }
                    else if (rpmFromWheels < s.redlineRpm * downAt && gear > 1) { gear--; shiftTimer = 0.18f; }
                }
            }
            else gear = 1;

            return totalRatio;
        }

        void ApplyDriveAndBrakes(float totalRatio, float dt)
        {
            var s = settings;

            // engine torque
            float torque = s.peakTorque * s.torqueCurve.Evaluate(EngineRpm) * driveInput;
            NitroActive = nitroInput && NitroCharge > 0.02f && !reversing && driveInput > 0.1f;
            if (NitroActive) torque *= s.nitroTorqueMultiplier;
            if (EngineRpm >= s.redlineRpm || shiftTimer > 0f) torque = 0f;

            float wheelTorqueTotal = torque * totalRatio * s.drivetrainEfficiency * (reversing ? -1f : 1f);

            int poweredCount = 0;
            for (int i = 0; i < wheels.Length; i++) if (wheels[i].powered) poweredCount++;
            float perWheel = poweredCount > 0 ? wheelTorqueTotal / poweredCount : 0f;

            // handbrake slide on the rear wheels
            handbrakeBlend = Mathf.MoveTowards(handbrakeBlend, handbrakeInput ? 1f : 0f, dt * 6f);

            float frontBrake = s.totalBrakeTorque * s.frontBrakeBias * 0.5f;
            float rearBrake = s.totalBrakeTorque * (1f - s.frontBrakeBias) * 0.5f;
            bool coasting = driveInput < 0.05f && brakeAmount < 0.05f;

            for (int i = 0; i < wheels.Length; i++)
            {
                var w = wheels[i];
                var c = w.collider;
                WheelHit hit;
                bool grounded = c.GetGroundHit(out hit);

                float motor = w.powered ? perWheel : 0f;
                if (w.powered && tractionControl && grounded && Mathf.Abs(motor) > 1f)
                {
                    float slip = Mathf.Abs(hit.forwardSlip);
                    if (slip > 0.55f) motor *= Mathf.Clamp01(1f - (slip - 0.55f) * 2.5f);
                }
                c.motorTorque = motor;

                float bt = brakeAmount * (i < 2 ? frontBrake : rearBrake);
                if (abs && bt > 0f && grounded && ForwardSpeed > 3f && hit.forwardSlip < -0.5f) bt *= 0.35f;
                if (w.handbrake && handbrakeInput) bt = Mathf.Max(bt, s.handbrakeTorque);
                if (coasting)
                {
                    if (Mathf.Abs(ForwardSpeed) < 0.5f) bt = Mathf.Max(bt, 500f);
                    else if (w.powered) bt += s.engineBrakeTorque;
                }
                c.brakeTorque = bt;

                if (w.handbrake)
                {
                    var sd = c.sidewaysFriction;
                    sd.stiffness = s.sidewaysStiffness * Mathf.Lerp(1f, s.handbrakeSlideFactor, handbrakeBlend);
                    c.sidewaysFriction = sd;
                }
            }
        }

        void ApplyAntiRoll(int l, int r)
        {
            var wl = wheels[l].collider; var wr = wheels[r].collider;
            WheelHit hit;
            float travelL = 1f, travelR = 1f;
            bool gl = wl.GetGroundHit(out hit);
            if (gl) travelL = (-wl.transform.InverseTransformPoint(hit.point).y - wl.radius) / wl.suspensionDistance;
            bool gr = wr.GetGroundHit(out hit);
            if (gr) travelR = (-wr.transform.InverseTransformPoint(hit.point).y - wr.radius) / wr.suspensionDistance;
            float force = (travelL - travelR) * settings.antiRoll;
            if (gl) Body.AddForceAtPosition(wl.transform.up * -force, wl.transform.position);
            if (gr) Body.AddForceAtPosition(wr.transform.up * force, wr.transform.position);
        }

        void ApplyAero(Vector3 v, float speed)
        {
            if (speed < 0.1f) return;
            var s = settings;
            Body.AddForce(-v.normalized * (0.5f * 1.225f * s.dragCoefficient * s.frontalArea * speed * speed));
            Body.AddForce(-transform.up * (s.downforce * speed * speed));
        }

        void UpdateNitro(float dt)
        {
            var s = settings;
            if (NitroActive) NitroCharge = Mathf.Max(0f, NitroCharge - dt / s.nitroDuration);
            else if (!nitroInput) NitroCharge = Mathf.Min(1f, NitroCharge + dt / s.nitroRecharge);
        }

        void CheckFlip(float dt, float speed)
        {
            if (transform.up.y < 0.25f && speed < 4f) flippedTimer += dt; else flippedTimer = 0f;
            if (flippedTimer > 2.5f) { flippedTimer = 0f; ResetUpright(); }
        }

        /// <summary>Put the car back on its wheels where it is, keeping its heading.</summary>
        public void ResetUpright()
        {
            float yaw = transform.eulerAngles.y;
            Respawn(transform.position + Vector3.up * 1.0f, Quaternion.Euler(0f, yaw, 0f));
        }

        public void Respawn(Vector3 position, Quaternion rotation)
        {
            Body.linearVelocity = Vector3.zero;
            Body.angularVelocity = Vector3.zero;
            Body.position = position;
            Body.rotation = rotation;
            transform.SetPositionAndRotation(position, rotation);
            gear = 1; reversing = false; shiftTimer = 0f;
            if (settings != null) EngineRpm = settings.idleRpm;
            for (int i = 0; wheels != null && i < wheels.Length; i++) { wheels[i].collider.motorTorque = 0f; wheels[i].collider.brakeTorque = 2000f; }
        }
    }
}
