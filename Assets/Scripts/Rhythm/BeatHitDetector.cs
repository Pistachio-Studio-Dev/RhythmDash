// [ Written by Jiayi ]

using System.Collections;
using DG.Tweening;
using Event;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatHitDetector : MonoBehaviour
{
    [SerializeField] private BeatSettings settings;

    private InputAction sprintAction;

    private bool windowOpen;
    private float windowTimer;

    private float windowOffset;

    private void Start()
    {
        windowOffset = GlobalConfig.Instance.BeatDelayDuration + settings.inputWindowOffset;
        sprintAction = InputSystem.actions["Sprint"];
        if (sprintAction == null) Debug.LogError("Sprint action not found");
    }

    private void Update()
    {
        if (GameplayClock.IsPaused)
        {
            return;
        }

        if (sprintAction == null)
        {
            return;
        }

        var triggered = sprintAction.WasPressedThisFrame();
        var success = false;

        if (windowOpen)
        {
            if (triggered)
            {
                Debug.Log("<color=green>Beat Hit success</color>");
                EventManager.TriggerEvent<BeatHit>();
                windowOpen = false;
                success = true;
            }

            windowTimer += Time.deltaTime;

            if (windowTimer >= settings.inputWindowDuration) windowOpen = false;
        }

        if (triggered && !success)
        {
            Debug.Log("<color=cyan>Beat Hit failed</color>");
            EventManager.TriggerEvent<BeatHitFailed>();
        }
    }

    private void OnEnable()
    {
        EventManager.StartListening<BeatAhead>(HandleBeat);
        EventManager.StartListening<Pause>(HandlePause);
    }

    private void OnDisable()
    {
        EventManager.StopListening<BeatAhead>(HandleBeat);
        EventManager.StopListening<Pause>(HandlePause);
    }

    private void HandleBeat()
    {
        DOTween.Sequence()
            .AppendInterval(windowOffset)
            .AppendCallback(() =>
            {
                if (GameplayClock.IsPaused)
                {
                    return;
                }

                windowOpen = true;
                windowTimer = 0f;
            });
    }

    private void HandlePause()
    {
        windowOpen = false;
        windowTimer = 0f;
    }
}
