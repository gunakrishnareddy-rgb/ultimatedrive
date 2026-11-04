using System;
using UnityEngine;
using UltimateDrive.InputSystemLayer;
using UltimateDrive.SaveSystem;
using UltimateDrive.Settings;
using UltimateDrive.Vehicle;

namespace UltimateDrive.CameraSystem
{
    public enum CameraView { Chase, Hood, Bumper }

    /// <summary>
    /// Phase 1 camera: smooth chase (leans into drifts, pulls back and widens FOV with speed and nitro, avoids scenery),
    /// plus hood and bumper views. C / gamepad Y / HUD button cycles. Part 6's VehicleCameraController replaces it later;
    /// never run both on one camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ChaseCamera : MonoBehaviour
    {
        public Transform target;
        public VehicleController car;
        public float distance = 6.4f, height = 2.0f, lookHeight = 1.1f;
        public float yawFollow = 5f, positionFollow = 18f, driftBlend = 0.45f;
        public float baseFov = 62f, speedFovGain = 20f, nitroFov = 7f, topSpeedMs = 75f;
        public LayerMask obstruction = ~0;

        public CameraView View { get; private set; }
        public static event Action<CameraView> ViewChanged;

        Camera cam;
        Vector3 smoothFwd = Vector3.forward;
        bool snap = true;

        void Awake() { cam = GetComponent<Camera>(); }

        void OnEnable()
        {
            DriveInputState.CameraPressed += Cycle;
            View = (CameraView)Mathf.Clamp(ProgressStore.Data.cameraView, 0, 2);
        }

        void OnDisable() { DriveInputState.CameraPressed -= Cycle; }

        public void SetTarget(VehicleController c)
        {
            car = c; target = c.transform;
            Vector3 f = target.forward; f.y = 0f;
            smoothFwd = f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            snap = true;
        }

        void Cycle()
        {
            if (Time.timeScale <= 0f) return;
            View = (CameraView)(((int)View + 1) % 3);
            ProgressStore.Data.cameraView = (int)View;
            ProgressStore.MarkDirty();
            snap = true;
            if (ViewChanged != null) ViewChanged(View);
        }

        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;
            Rigidbody body = car != null ? car.Body : null;
            Vector3 vel = body != null ? body.linearVelocity : Vector3.zero;
            float speed = vel.magnitude;
            float speed01 = Mathf.Clamp01(speed / topSpeedMs);
            bool nitro = car != null && car.NitroActive;

            float fovBase = View == CameraView.Chase ? baseFov : (View == CameraView.Hood ? 72f : 80f);
            float fovTarget = fovBase + speedFovGain * speed01 * speed01 + (nitro ? nitroFov : 0f);
            cam.fieldOfView = snap ? fovTarget : Mathf.Lerp(cam.fieldOfView, fovTarget, 1f - Mathf.Exp(-6f * dt));

            if (View == CameraView.Chase) UpdateChase(dt, body, vel, speed, speed01, nitro);
            else UpdateMounted(dt);
            snap = false;
        }

        void UpdateChase(float dt, Rigidbody body, Vector3 vel, float speed, float speed01, bool nitro)
        {
            Vector3 fwd = target.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.001f) fwd = smoothFwd;
            fwd.Normalize();

            vel.y = 0f;
            if (speed > 6f && vel.sqrMagnitude > 0.01f)
            {
                Vector3 vd = vel.normalized;
                if (Vector3.Dot(vd, fwd) > 0.2f) fwd = Vector3.Slerp(fwd, vd, driftBlend * Mathf.Clamp01((speed - 6f) / 20f));
            }
            smoothFwd = snap ? fwd : Vector3.Slerp(smoothFwd, fwd, 1f - Mathf.Exp(-yawFollow * dt));

            float dist = distance + speed01 * 1.8f + (nitro ? 0.8f : 0f);
            float h = height + speed01 * 0.25f;
            Vector3 pivot = target.position + Vector3.up * lookHeight;
            Vector3 wanted = target.position - smoothFwd * dist + Vector3.up * h;

            Vector3 dir = wanted - pivot; float len = dir.magnitude;
            if (len > 0.01f)
            {
                RaycastHit hit;
                if (Physics.SphereCast(pivot, 0.3f, dir / len, out hit, len, obstruction, QueryTriggerInteraction.Ignore)
                    && hit.collider.attachedRigidbody != body)
                    wanted = pivot + dir / len * Mathf.Max(0.8f, hit.distance);
            }
            if (wanted.y < 0.4f) wanted.y = 0.4f;

            transform.position = snap ? wanted : Vector3.Lerp(transform.position, wanted, 1f - Mathf.Exp(-positionFollow * dt));
            Vector3 lookPoint = pivot + smoothFwd * (2f + speed01 * 5f);
            Quaternion look = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            transform.rotation = snap ? look : Quaternion.Slerp(transform.rotation, look, 1f - Mathf.Exp(-14f * dt));
        }

        void UpdateMounted(float dt)
        {
            Vector3 local = View == CameraView.Hood ? new Vector3(0f, 1.02f, 1.0f) : new Vector3(0f, 0.5f, 2.3f);
            transform.position = target.TransformPoint(local);
            Quaternion want = target.rotation * Quaternion.Euler(View == CameraView.Hood ? 3f : 0f, 0f, 0f);
            transform.rotation = snap ? want : Quaternion.Slerp(transform.rotation, want, 1f - Mathf.Exp(-30f * dt));
        }
    }
}
