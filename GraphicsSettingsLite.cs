using System;
using UnityEngine;
using UltimateDrive.SaveSystem;

namespace UltimateDrive.Settings
{
    public enum GraphicsPreset { Auto, Low, Medium, Ultra }

    /// <summary>
    /// Phase 1 graphics presets (Low / Medium / Ultra + Auto). Matches a Quality level by name when one exists,
    /// then sets the Quality values that apply on every pipeline. Gameplay never depends on the preset.
    /// The full Part 4/17 system (URP assets, render scale, adaptive quality) replaces this later.
    /// </summary>
    public static class GraphicsSettingsLite
    {
        public static GraphicsPreset Current
        {
            get
            {
                GraphicsPreset p;
                return Enum.TryParse(ProgressStore.Data.graphicsPreset, out p) ? p : GraphicsPreset.Auto;
            }
        }

        public static GraphicsPreset Effective { get { var c = Current; return c == GraphicsPreset.Auto ? Recommended() : c; } }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init() { Apply(Current); ApplyFrameRate(); }

        public static GraphicsPreset Recommended()
        {
#if UNITY_WEBGL
            // browsers hide real hardware info: phones get Low, desktops Medium (Ultra is a manual choice)
            if (UltimateDrive.Core.PlatformInfo.IsTouch) return GraphicsPreset.Low;
            return GraphicsPreset.Medium;
#endif
            int score = 0;
            int ram = SystemInfo.systemMemorySize;       // MB
            int vram = SystemInfo.graphicsMemorySize;    // MB
            int cores = SystemInfo.processorCount;
            if (ram >= 3000) score += 15;
            if (ram >= 6000) score += 15;
            if (vram >= 1500) score += 10;
            if (vram >= 4000) score += 10;
            if (cores >= 4) score += 10;
            if (cores >= 8) score += 10;
            if (SystemInfo.supportsComputeShaders) score += 10;
            if (SystemInfo.systemMemorySize < 2500 || !SystemInfo.supportsComputeShaders) return GraphicsPreset.Low;
            bool mobile = Application.isMobilePlatform;
            int ultraAt = mobile ? 80 : 70, mediumAt = mobile ? 45 : 40;
            if (score >= ultraAt) return GraphicsPreset.Ultra;
            if (score >= mediumAt) return GraphicsPreset.Medium;
            return GraphicsPreset.Low;
        }

        public static void Set(GraphicsPreset preset)
        {
            ProgressStore.Data.graphicsPreset = preset.ToString();
            ProgressStore.MarkDirty();
            Apply(preset);
        }

        public static GraphicsPreset Cycle()
        {
            var next = (GraphicsPreset)(((int)Current + 1) % 4);
            Set(next);
            return next;
        }

        public static void Apply(GraphicsPreset preset)
        {
            if (preset == GraphicsPreset.Auto) preset = Recommended();
            int idx = Array.IndexOf(QualitySettings.names, preset.ToString());
            if (idx >= 0) QualitySettings.SetQualityLevel(idx, true);

            switch (preset)
            {
                case GraphicsPreset.Low:
                    QualitySettings.shadowDistance = 35f;
                    QualitySettings.shadowCascades = 1;
                    QualitySettings.antiAliasing = 0;
                    QualitySettings.lodBias = 0.6f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    QualitySettings.pixelLightCount = 1;
                    break;
                case GraphicsPreset.Medium:
                    QualitySettings.shadowDistance = 90f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.antiAliasing = 2;
                    QualitySettings.lodBias = 1f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.pixelLightCount = 2;
                    break;
                default:
                    QualitySettings.shadowDistance = 200f;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.lodBias = 1.6f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.pixelLightCount = 4;
                    break;
            }
        }

        public static void ApplyFrameRate()
        {
#if UNITY_WEBGL
            return;   // the browser drives the frame rate
#endif
            if (Application.isMobilePlatform)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = Mathf.Clamp(ProgressStore.Data.targetFps, 30, 120);
            }
            else
            {
                QualitySettings.vSyncCount = 1;
                Application.targetFrameRate = -1;
            }
        }

        public static int CycleFps()
        {
            int f = ProgressStore.Data.targetFps;
            f = f <= 30 ? 45 : (f <= 45 ? 60 : 30);
            ProgressStore.Data.targetFps = f;
            ProgressStore.MarkDirty();
            ApplyFrameRate();
            return f;
        }
    }
}
