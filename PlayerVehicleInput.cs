using UnityEngine;
using UltimateDrive.InputSystemLayer;

namespace UltimateDrive.Vehicle
{
    /// <summary>Feeds DriveInputState (keyboard, gamepad or touch) into the car.</summary>
    [RequireComponent(typeof(VehicleController))]
    public class PlayerVehicleInput : MonoBehaviour
    {
        VehicleController car;

        void Awake() { car = GetComponent<VehicleController>(); }
        void OnEnable() { DriveInputState.ResetPressed += OnReset; }
        void OnDisable() { DriveInputState.ResetPressed -= OnReset; }

        void OnReset() { if (car != null && Time.timeScale > 0f) car.ResetUpright(); }

        void Update()
        {
            car.steerInput = DriveInputState.Steer;
            car.throttleInput = DriveInputState.Throttle;
            car.brakeInput = DriveInputState.Brake;
            car.handbrakeInput = DriveInputState.Handbrake;
            car.nitroInput = DriveInputState.Nitro;
        }
    }
}
