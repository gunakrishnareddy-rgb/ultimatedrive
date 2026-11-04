using System;
using System.IO;
using UnityEngine;

namespace UltimateDrive.SaveSystem
{
    [Serializable]
    public class ProgressData
    {
        public int version = 1;
        public int coins;
        public float totalDistanceKm;
        public float bestLapSeconds;
        public int lapsCompleted;
        public string selectedCarId = "apex_gt";
        public string graphicsPreset = "Auto";
        public int targetFps = 60;
        public int cameraView;
        public float masterVolume = 1f;
    }

    /// <summary>
    /// Offline progress file in persistentDataPath. Atomic write (temp file, then swap) with a .bak copy,
    /// so a crash or power cut cannot destroy the previous save. This is the Phase 1 store; Part 16's
    /// SaveManager (encrypted, profiles) replaces it later.
    /// </summary>
    public static class ProgressStore
    {
        static ProgressData data;
        static bool dirty;
        public static event Action CoinsChanged;

        static string Dir { get { return Path.Combine(Application.persistentDataPath, "UltimateDrive"); } }
        static string FilePath { get { return Path.Combine(Dir, "progress.json"); } }

        public static ProgressData Data
        {
            get { if (data == null) Load(); return data; }
        }

        public static void Load()
        {
            data = TryRead(FilePath);
            if (data == null) data = TryRead(FilePath + ".bak");
            if (data == null) data = new ProgressData();
        }

        static ProgressData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<ProgressData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Could not read " + path + ": " + e.Message);
                return null;
            }
        }

        public static void MarkDirty() { dirty = true; }

        public static void SaveIfDirty() { if (dirty) Save(); }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                Directory.CreateDirectory(Dir);
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data));
                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, FilePath + ".bak", true);
                    File.Delete(FilePath);
                }
                File.Move(tmp, FilePath);
                dirty = false;
                UltimateDrive.Core.PlatformInfo.FlushSaves();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] Write failed (will retry): " + e.Message);
            }
        }

        public static void AddCoins(int amount)
        {
            if (amount == 0) return;
            Data.coins = Mathf.Max(0, Data.coins + amount);
            dirty = true;
            if (CoinsChanged != null) CoinsChanged();
        }
    }

    /// <summary>Hidden object that flushes the save on pause / focus loss / quit (Android can kill the app without a quit event).</summary>
    public class ProgressSaveRunner : MonoBehaviour
    {
        float timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("ProgressSaveRunner");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<ProgressSaveRunner>();
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer > 20f) { timer = 0f; ProgressStore.SaveIfDirty(); }
        }

        void OnApplicationPause(bool paused) { if (paused) ProgressStore.Save(); }
        void OnApplicationFocus(bool focus) { if (!focus) ProgressStore.Save(); }
        void OnApplicationQuit() { ProgressStore.Save(); }
    }
}
