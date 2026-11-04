using UnityEngine;
using UltimateDrive.Core;
using UltimateDrive.Settings;
using UltimateDrive.UI;

namespace UltimateDrive.Vehicle
{
    /// <summary>Skid marks, tyre smoke (scaled by graphics preset) and brake lights.</summary>
    public class VehicleEffects : MonoBehaviour
    {
        VehicleController car;
        Material tail;
        Color tailDim = new Color(0.55f, 0.03f, 0.03f), tailBright = new Color(1f, 0.08f, 0.08f);
        TrailRenderer[] skids = new TrailRenderer[4];
        ParticleSystem[] smoke = new ParticleSystem[4];
        float density = 1f;
        bool lastBrake;

        public void Init(VehicleController vc, Material tailMaterial)
        {
            car = vc; tail = tailMaterial;
            var preset = GraphicsSettingsLite.Effective;
            density = preset == GraphicsPreset.Low ? 0.35f : (preset == GraphicsPreset.Medium ? 0.7f : 1f);

            var trailMat = new Material(Shader.Find("Sprites/Default"));
            var smokeMat = new Material(Shader.Find("Sprites/Default"));
            smokeMat.mainTexture = UISprites.Circle.texture;

            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject("Skid" + i);
                go.transform.SetParent(transform, false);
                var tr = go.AddComponent<TrailRenderer>();
                tr.sharedMaterial = trailMat;
                tr.time = preset == GraphicsPreset.Low ? 3f : 8f;
                tr.minVertexDistance = 0.25f;
                tr.widthMultiplier = 0.27f;
                tr.alignment = LineAlignment.TransformZ;
                tr.startColor = new Color(0.02f, 0.02f, 0.02f, 0.55f);
                tr.endColor = new Color(0.02f, 0.02f, 0.02f, 0f);
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.emitting = false;
                skids[i] = tr;

                smoke[i] = MakeSmoke(smokeMat);
            }
        }

        ParticleSystem MakeSmoke(Material mat)
        {
            var go = new GameObject("TyreSmoke");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.startLifetime = 1.3f; main.startSpeed = 0.7f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
            main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.4f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.04f;
            main.maxParticles = Mathf.RoundToInt(100f * density) + 10;
            var em = ps.emission; em.rateOverTime = 0f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.15f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f, 0.7f, 0.7f), 1f) },
                      new[] { new GradientAlphaKey(0.55f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 2.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        void LateUpdate()
        {
            if (car == null) return;
            for (int i = 0; i < 4; i++)
            {
                var c = car.wheels[i].collider;
                WheelHit hit;
                bool grounded = c.GetGroundHit(out hit);
                float slip = grounded ? Mathf.Max(Mathf.Abs(hit.forwardSlip), Mathf.Abs(hit.sidewaysSlip)) : 0f;
                bool skidding = grounded && slip > 0.6f && car.SpeedKmh > 8f;

                if (grounded)
                {
                    skids[i].transform.SetPositionAndRotation(hit.point + Vector3.up * 0.03f, Quaternion.LookRotation(Vector3.up, transform.forward));
                    smoke[i].transform.position = hit.point + Vector3.up * 0.1f;
                }
                skids[i].emitting = skidding;
                var em = smoke[i].emission;
                em.rateOverTime = skidding ? Mathf.Clamp((slip - 0.5f) * 60f, 5f, 40f) * density : 0f;
            }

            bool braking = car.BrakeAmount > 0.1f || car.Reversing;
            if (braking != lastBrake && tail != null)
            {
                MaterialFactory.SetColor(tail, braking ? tailBright : tailDim);
                lastBrake = braking;
            }
        }
    }
}
