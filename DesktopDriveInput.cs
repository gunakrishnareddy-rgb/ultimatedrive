using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UltimateDrive.InputSystemLayer
{
    /// <summary>
    /// Windows keyboard + gamepad. Works with Active Input Handling = Input System Package, Both, or the old Input Manager.
    /// W/Up gas, S/Down brake+reverse, A/D or arrows steer, Space handbrake, Shift nitro, C camera, R reset, Esc pause.
    /// Gamepad: RT/LT, left stick, A handbrake, B nitro, Y camera, X reset, Start pause.
    /// </summary>
    public class DesktopDriveInput : MonoBehaviour
    {
        public float keyboardSteerRate = 4.5f;     // units/second toward the pressed direction
        public float keyboardSteerReturn = 7f;     // units/second back to centre
        public float stickDeadzone = 0.12f;

        float steerSmoothed;

        void OnDisable() { DriveInputState.Clear(); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float keySteer = 0f, thr = 0f, brk = 0f;
            bool hand = false, nitro = false;
            float padSteer = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) keySteer -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) keySteer += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) thr = 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) brk = 1f;
                hand |= kb.spaceKey.isPressed;
                nitro |= kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                if (kb.cKey.wasPressedThisFrame) DriveInputState.RaiseCamera();
                if (kb.rKey.wasPressedThisFrame) DriveInputState.RaiseReset();
                if (kb.escapeKey.wasPressedThisFrame) DriveInputState.RaisePause();
            }
            var gp = Gamepad.current;
            if (gp != null)
            {
                float sx = gp.leftStick.x.ReadValue();
                if (Mathf.Abs(sx) > stickDeadzone) padSteer = sx;
                thr = Mathf.Max(thr, gp.rightTrigger.ReadValue());
                brk = Mathf.Max(brk, gp.leftTrigger.ReadValue());
                hand |= gp.buttonSouth.isPressed;
                nitro |= gp.buttonEast.isPressed;
                if (gp.buttonNorth.wasPressedThisFrame) DriveInputState.RaiseCamera();
                if (gp.buttonWest.wasPressedThisFrame) DriveInputState.RaiseReset();
                if (gp.startButton.wasPressedThisFrame) DriveInputState.RaisePause();
            }
#else
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keySteer -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keySteer += 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) thr = 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) brk = 1f;
            hand = Input.GetKey(KeyCode.Space);
            nitro = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (Input.GetKeyDown(KeyCode.C)) DriveInputState.RaiseCamera();
            if (Input.GetKeyDown(KeyCode.R)) DriveInputState.RaiseReset();
            if (Input.GetKeyDown(KeyCode.Escape)) DriveInputState.RaisePause();
#endif
            // Keyboard steering ramps so a tap is a small input and a hold is full lock.
            float rate = Mathf.Abs(keySteer) > 0.01f ? keyboardSteerRate : keyboardSteerReturn;
            steerSmoothed = Mathf.MoveTowards(steerSmoothed, keySteer, rate * dt);

            DriveInputState.Steer = Mathf.Clamp(Mathf.Abs(padSteer) > 0.01f ? padSteer : steerSmoothed, -1f, 1f);
            DriveInputState.Throttle = thr;
            DriveInputState.Brake = brk;
            DriveInputState.Handbrake = hand;
            DriveInputState.Nitro = nitro;
        }
    }
}
