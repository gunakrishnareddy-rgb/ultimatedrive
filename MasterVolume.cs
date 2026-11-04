using UnityEngine;
using UltimateDrive.SaveSystem;

namespace UltimateDrive.Audio
{
    /// <summary>Saved master volume (steps 100/75/50/25/0 %).</summary>
    public static class MasterVolume
    {
        static readonly float[] Steps = { 1f, 0.75f, 0.5f, 0.25f, 0f };

        public static void Apply() { AudioListener.volume = ProgressStore.Data.masterVolume; }

        public static float Cycle()
        {
            float cur = ProgressStore.Data.masterVolume;
            int next = 0;
            for (int i = 0; i < Steps.Length; i++) if (Mathf.Approximately(Steps[i], cur)) { next = (i + 1) % Steps.Length; break; }
            ProgressStore.Data.masterVolume = Steps[next];
            ProgressStore.MarkDirty();
            Apply();
            return Steps[next];
        }

        public static string Label() { return Mathf.RoundToInt(ProgressStore.Data.masterVolume * 100f) + "%"; }
    }
}
