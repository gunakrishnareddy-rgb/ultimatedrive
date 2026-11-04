using System;
using UnityEngine;
using UltimateDrive.SaveSystem;
using UltimateDrive.Vehicle;

namespace UltimateDrive.World
{
    /// <summary>
    /// Counts laps by following the car's progress along the circuit path, so cutting across the infield does not count.
    /// Pays coins for laps and distance, stores the best lap, and puts the car back on the road if it falls off the world.
    /// </summary>
    public class LapTimer : MonoBehaviour
    {
        public VehicleController car;
        public Vector3[] path;
        public int startOffset = 8;            // the car spawns this many path points before the start line

        public float LapTime { get; private set; }
        public float LastLap { get; private set; }
        public float BestLap { get; private set; }
        public int Laps { get; private set; }
        public bool Started { get; private set; }

        /// <summary>lap seconds, is new best, coins earned</summary>
        public event Action<float, bool, int> LapCompleted;

        int n, lastIdx;
        float progress, distAccum;

        void Start()
        {
            n = path.Length;
            BestLap = ProgressStore.Data.bestLapSeconds;
            lastIdx = Nearest(car.transform.position, -1);
            progress = -startOffset;
        }

        int Nearest(Vector3 p, int around)
        {
            int best = 0; float bd = float.MaxValue;
            if (around < 0)
            {
                for (int i = 0; i < n; i++) { float d = Sq(p, path[i]); if (d < bd) { bd = d; best = i; } }
                return best;
            }
            for (int k = -8; k <= 8; k++)
            {
                int i = ((around + k) % n + n) % n;
                float d = Sq(p, path[i]);
                if (d < bd) { bd = d; best = i; }
            }
            if (bd > 30f * 30f) return Nearest(p, -1);     // far from where we were: search everywhere
            return best;
        }

        static float Sq(Vector3 a, Vector3 b) { float dx = a.x - b.x, dz = a.z - b.z; return dx * dx + dz * dz; }

        void Update()
        {
            if (car == null || n == 0) return;
            float dt = Time.deltaTime;
            Vector3 pos = car.transform.position;

            if (pos.y < -8f) { RespawnOnTrack(); return; }

            int idx = Nearest(pos, lastIdx);
            int delta = idx - lastIdx;
            if (delta > n / 2) delta -= n; else if (delta < -n / 2) delta += n;
            if (Mathf.Abs(delta) <= 12) progress += delta;     // big jumps (teleport, shortcut) add nothing
            lastIdx = idx;

            if (!Started && progress >= 0f) { Started = true; LapTime = 0f; }
            if (Started)
            {
                LapTime += dt;
                if (progress >= n)
                {
                    progress -= n;
                    CompleteLap();
                }
                else if (progress < -n * 0.25f) { progress = 0f; LapTime = 0f; }   // drove backwards a long way
            }

            // distance and coins
            float step = Mathf.Max(0f, car.ForwardSpeed) * dt;
            distAccum += step;
            ProgressStore.Data.totalDistanceKm += step / 1000f;
            if (distAccum >= 250f) { distAccum -= 250f; ProgressStore.AddCoins(5); }
        }

        void CompleteLap()
        {
            float t = LapTime;
            Laps++; LastLap = t; LapTime = 0f;
            bool best = BestLap <= 0f || t < BestLap;
            if (best) { BestLap = t; ProgressStore.Data.bestLapSeconds = t; }
            int coins = 100 + (best ? 150 : 0);
            ProgressStore.Data.lapsCompleted++;
            ProgressStore.AddCoins(coins);
            ProgressStore.Save();
            if (LapCompleted != null) LapCompleted(t, best, coins);
        }

        void RespawnOnTrack()
        {
            int i = ((lastIdx % n) + n) % n;
            Vector3 t = TrackBuilder.Tangent(path, i);
            car.Respawn(path[i] + Vector3.up * 0.7f, Quaternion.LookRotation(t));
        }
    }
}
