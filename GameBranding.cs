namespace UltimateDrive.Core
{
    /// <summary>Single source for the game's name, tagline and scene names.</summary>
    public static class GameBranding
    {
        public const string Title = "ULTIMATE DRIVE";
        public const string Tagline = "DRIVE BEYOND LIMITS. EXPERIENCE THE FREEDOM.";
        public const string Version = "0.1.0 (prototype)";

        public const string BootScene = "Boot";
        public const string MenuScene = "MainMenu";
        public const string DriveScene = "PrototypeDrive";

        // Palette from the README (matte black, electric blue, neon orange).
        public static readonly UnityEngine.Color Blue = new UnityEngine.Color(0.10f, 0.55f, 1f, 1f);
        public static readonly UnityEngine.Color Orange = new UnityEngine.Color(1f, 0.45f, 0.08f, 1f);
        public static readonly UnityEngine.Color Black = new UnityEngine.Color(0.04f, 0.045f, 0.055f, 1f);
    }
}
