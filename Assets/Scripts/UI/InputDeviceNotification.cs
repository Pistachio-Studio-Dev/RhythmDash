using UnityEngine;
using UnityEngine.InputSystem;


public class InputDeviceNotification : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad || device is Joystick)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    NotificationCenter.Instance?.ShowNotification($"Controller Connected\n{device.displayName}");
                    break;
                case InputDeviceChange.Removed:
                    NotificationCenter.Instance?.ShowNotification($"Controller Disconnected\n{device.displayName}");
                    break;
            }
        }
    }
}
