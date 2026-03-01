// [ Written by Jiayi ]

using Event;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class BeatDetector : MonoBehaviour
{
    private struct AmplitudeRecord
    {
        public float amplitude;
        public float time;
    }

    [SerializeField] private BeatSettings settings;
    private float cooldownTimer;
    
    private Queue<AmplitudeRecord> history;
    private bool historyFull;
    private float historySum;

    private void Awake()
    {
        history = new Queue<AmplitudeRecord>();
    }

    private void Update()
    {
        DetectBeat();
    }

    private void DetectBeat()
    {
        var amplitude = AudioAnalyzer.Instance.AheadNormalizedValue;
        var currentTime = Time.time;

        history.Enqueue(new AmplitudeRecord { amplitude = amplitude, time = currentTime });
        historySum += amplitude;
        
        if (!historyFull)
        {
            if (currentTime - history.Peek().time >= settings.historyTimeWindow)
                historyFull = true;
            else
                return;
        }

        while (history.Count > 0 && currentTime - history.Peek().time > settings.historyTimeWindow)
        {
            historySum -= history.Dequeue().amplitude;
        }

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        var average = history.Count > 0 ? historySum / history.Count : 0f;
        var threshold = average * settings.beatThreshold;

        if (average > 0f && amplitude > threshold)
        {
            cooldownTimer = settings.cooldownTime;
            
            EventManager.TriggerEvent<BeatAhead>();
            EventManager.TriggerEvent<BeatAheadWithAmplitude, float>(amplitude);

            DOTween.Sequence()
                .AppendInterval(GlobalConfig.Instance.BeatDelayDuration)
                .AppendCallback(() =>
                {
                    // Debug.Log($"Beat Detected with amplitude {amplitude}");
                    EventManager.TriggerEvent<Beat>();
                    EventManager.TriggerEvent<BeatWithAmplitude, float>(amplitude);
                });
        }
    }
}