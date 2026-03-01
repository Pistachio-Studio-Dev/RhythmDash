using System.Collections;
using System;
using DG.Tweening;
using Event;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;

public class BeatControllerVibration : MonoBehaviour
{
    private Tween vibrationTween;

    private void OnEnable()
    {
        EventManager.StartListening<BeatWithAmplitude, float>(HandleBeat);
    }

    private void OnDisable()
    {
        EventManager.StopListening<BeatWithAmplitude, float>(HandleBeat);
        
        vibrationTween?.Kill();
        vibrationTween = null;
        
        foreach (var gamepad in Gamepad.all)
        {
            gamepad.ResetHaptics();
        }
    }

    private void HandleBeat(float amplitude)
    {
        var gamepad = Gamepad.current;
        var config = GlobalConfig.Instance;

        if (gamepad == null || config == null)
        {
            return;
        }


        float duration = GlobalConfig.Instance.BeatVibrationDuration;
        float strengthLow = GlobalConfig.Instance.BeatVibrationStrengthLow;
        float strengthHigh = GlobalConfig.Instance.BeatVibrationStrengthHigh;

        // dualsense controller has strong vibration motors
        if (gamepad is DualSenseGamepadHID)
        {
            duration = GlobalConfig.Instance.DualSenseBeatVibrationDuration;
            strengthLow = GlobalConfig.Instance.DualSenseBeatVibrationStrengthLow;
            strengthHigh = GlobalConfig.Instance.DualSenseBeatVibrationStrengthHigh;
        }
        
        duration = Mathf.Max(0f, duration);
        strengthLow = Mathf.Clamp01(strengthLow * amplitude);
        strengthHigh = Mathf.Clamp01(strengthHigh * amplitude);

        if (duration <= 0f || (strengthLow <= 0f && strengthHigh <= 0f))
        {
            gamepad.ResetHaptics();
            return;
        }

        vibrationTween?.Kill();

        vibrationTween = DOTween.Sequence()
            .AppendCallback(() =>
            {
                gamepad.SetMotorSpeeds(strengthLow, strengthHigh);
                (gamepad as DualSenseGamepadHID)?.SetLightBarColor(Color.black);
            })
            .AppendInterval(duration)
            .AppendCallback(() =>
            {
                gamepad.ResetHaptics();
                (gamepad as DualSenseGamepadHID)?.SetLightBarColor(Color.cyan);
                vibrationTween = null;
            });
    }
}
