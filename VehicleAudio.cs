using UnityEngine;
using UltimateDrive.Vehicle;

namespace UltimateDrive.Audio
{
    /// <summary>
    /// Engine, tyre screech, wind and crash sounds, all generated in code (no audio files, nothing to download).
    /// Two pitched engine layers crossfade over the rev range. Part 8's VehicleSoundController replaces this later.
    /// </summary>
    public class VehicleAudio : MonoBehaviour
    {
        const int Rate = 44100;
        static AudioClip engineLow, engineHigh, tyreClip, windClip, thumpClip;

        VehicleController car;
        AudioSource low, high, tyre, wind, oneShot;
        bool player;

        public static float EngineVolume = 0.55f, SfxVolume = 0.8f;

        public void Init(VehicleController vc, bool isPlayer)
        {
            car = vc; player = isPlayer;
            BuildClips();
            low = Make(engineLow, true); high = Make(engineHigh, true);
            tyre = Make(tyreClip, true); wind = Make(windClip, true);
            oneShot = Make(null, false);
            low.Play(); high.Play(); tyre.Play(); wind.Play();
            tyre.volume = 0f; wind.volume = 0f;
        }

        AudioSource Make(AudioClip clip, bool loop)
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.clip = clip; a.loop = loop; a.playOnAwake = false;
            a.spatialBlend = player ? 0f : 1f;
            a.minDistance = 8f; a.maxDistance = 120f;
            a.dopplerLevel = 0.3f;
            return a;
        }

        static void BuildClips()
        {
            if (engineLow != null) return;
            engineLow = EngineClip("EngineLow", 48f, 1.5f);
            engineHigh = EngineClip("EngineHigh", 105f, 1.1f);
            tyreClip = NoiseClip("Tyre", 2f, 0.55f, 0.08f);
            windClip = NoiseClip("Wind", 2f, 0.06f, 0.0f);
            thumpClip = ThumpClip();
        }

        // additive harmonics with a touch of noise, soft-clipped: exactly periodic so it loops without a click
        static AudioClip EngineClip(string name, float baseHz, float drive)
        {
            int n = Rate; var d = new float[n];
            var rng = new System.Random(7);
            var amps = new float[15];
            for (int h = 1; h <= 14; h++) amps[h] = (1f / Mathf.Pow(h, 0.85f)) * (h % 2 == 0 ? 0.7f : 1f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate, s = 0f;
                for (int h = 1; h <= 14; h++)
                    s += amps[h] * Mathf.Sin(2f * Mathf.PI * baseHz * h * t + h * 0.7f);
                s += ((float)rng.NextDouble() - 0.5f) * 0.12f;
                // slow amplitude wobble that also repeats every second: the idle "lope"
                s *= 1f + 0.12f * Mathf.Sin(2f * Mathf.PI * 6f * t);
                d[i] = (float)System.Math.Tanh(s * 0.45f * drive) * 0.7f;
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        // filtered noise; lowpass coefficient 0..1 (higher = brighter), highpass removes rumble
        static AudioClip NoiseClip(string name, float seconds, float lowpass, float highpass)
        {
            int n = (int)(Rate * seconds); var d = new float[n];
            var rng = new System.Random(name.Length * 31);
            float lp = 0f, hpState = 0f;
            for (int i = 0; i < n; i++)
            {
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * lowpass;
                float hp = lp - hpState; hpState += hp * highpass;
                d[i] = hp * 0.8f;
            }
            int fade = 2000;                                  // crossfade the loop seam
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                d[i] = d[i] * k + d[n - fade + i] * (1f - k);
            }
            var c = AudioClip.Create(name, n - fade, 1, Rate, false);
            var trimmed = new float[n - fade]; System.Array.Copy(d, trimmed, n - fade);
            c.SetData(trimmed, 0);
            return c;
        }

        static AudioClip ThumpClip()
        {
            int n = (int)(Rate * 0.45f); var d = new float[n];
            var rng = new System.Random(3); float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float w = (float)rng.NextDouble() * 2f - 1f;
                lp += (w - lp) * 0.12f;
                d[i] = (lp * 1.4f + Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.6f) * Mathf.Exp(-t * 9f);
            }
            var c = AudioClip.Create("Thump", n, 1, Rate, false); c.SetData(d, 0); return c;
        }

        void Update()
        {
            if (car == null) return;
            float rpm01 = car.Rpm01;
            float thr = Mathf.Clamp01(car.throttleInput);
            float load = Mathf.Lerp(0.5f, 1f, thr);

            low.pitch = Mathf.Lerp(0.8f, 2.4f, rpm01);
            high.pitch = Mathf.Lerp(0.75f, 2.2f, rpm01);
            low.volume = EngineVolume * load * Mathf.Pow(1f - rpm01, 0.5f) * 0.9f;
            high.volume = EngineVolume * load * Mathf.Pow(rpm01, 0.7f);
            if (car.NitroActive) high.pitch *= 1.05f;

            float speed = car.SpeedKmh;
            wind.volume = Mathf.Clamp01(speed / 260f) * 0.5f * SfxVolume;
            wind.pitch = Mathf.Lerp(0.7f, 1.4f, Mathf.Clamp01(speed / 260f));

            float slip = 0f;
            for (int i = 0; i < 4; i++)
            {
                WheelHit h;
                if (car.wheels[i].collider.GetGroundHit(out h))
                    slip = Mathf.Max(slip, Mathf.Abs(h.forwardSlip), Mathf.Abs(h.sidewaysSlip));
            }
            float target = (speed > 10f) ? Mathf.Clamp01((slip - 0.55f) * 2.2f) * 0.55f * SfxVolume : 0f;
            tyre.volume = Mathf.MoveTowards(tyre.volume, target, Time.deltaTime * 4f);
        }

        void OnCollisionEnter(Collision c)
        {
            if (oneShot == null) return;
            float v = c.relativeVelocity.magnitude;
            if (v < 3f) return;
            oneShot.pitch = Random.Range(0.85f, 1.1f);
            oneShot.PlayOneShot(thumpClip, Mathf.Clamp01(v / 18f) * SfxVolume);
        }
    }
}
