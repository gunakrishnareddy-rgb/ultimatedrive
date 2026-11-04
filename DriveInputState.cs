using System;

namespace UltimateDrive.InputSystemLayer
{
    /// <summary>
    /// The one shared output of every input source (keyboard, gamepad, touch HUD, AI).
    /// The car reads only this. Sources write it every frame.
    /// </summary>
    public static class DriveInputState
    {
        public static float Steer;      // -1 left .. +1 right
        public static float Throttle;   // 0..1
        public static float Brake;      // 0..1 (also reverse from standstill)
        public static bool Handbrake;
        public static bool Nitro;

        public static event Action CameraPressed;
        public static event Action ResetPressed;
        public static event Action PausePressed;

        public static void RaiseCamera() { if (CameraPressed != null) CameraPressed(); }
        public static void RaiseReset() { if (ResetPressed != null) ResetPressed(); }
        public static void RaisePause() { if (PausePressed != null) PausePressed(); }

        public static void Clear()
        {
            Steer = 0f; Throttle = 0f; Brake = 0f; Handbrake = false; Nitro = false;
        }
    }
}
