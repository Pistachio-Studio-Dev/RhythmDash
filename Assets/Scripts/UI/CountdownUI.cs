// [ Written by Lindsey, modified by Jiayi ]

using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using Event;
using UnityEngine.UI;

public class CountdownUI : SessionBasedBehaviour
{
    private int countFrom = 3;
    [SerializeField] private TMP_Text countdownNumberTMP;
    private Coroutine countdownRoutine;
    
    [SerializeField] private Image[] leftLights;
    [SerializeField] private Image[] rightLights;
    [SerializeField] private Color targetLightColor = Color.green;

    private void Awake() {
        if (countdownNumberTMP == null)
        {
            Debug.LogError("CountdownUI: No Text component found in children.");
        }
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        StopCountdown();
        
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(newState == SessionState.Countdown);
        }

        if (newState == SessionState.Countdown) {
            countdownRoutine = StartCoroutine(CountdownCoroutine());
        }
    }

    private IEnumerator CountdownCoroutine()
    {
        // countdownNumberTMP.transform.DOScale(Vector3.one * 1.5f, countFrom).SetEase(Ease.InCirc);
        
        double endTime = GameplayClock.TimeAsDouble + countFrom;

        while (currentSessionState == SessionState.Countdown)
        {
            double remaining = endTime - GameplayClock.TimeAsDouble;
            if (remaining <= 0d)
            {
                break;
            }

            SoundManager.Instance?.Play("Countdown");
            countdownNumberTMP.text = Mathf.Ceil((float)remaining).ToString();
            DOTween.Sequence()
                .Append(countdownNumberTMP.rectTransform.DOScale(1.7f * Vector3.one, 0.15f).SetEase(Ease.OutCubic))
                .Append(countdownNumberTMP.rectTransform.DOScale(1f * Vector3.one, 0.15f).SetEase(Ease.OutCubic));
            
            yield return GameplayClock.WaitForSeconds(1);

            int lightIndex = 3 - (int)Mathf.Ceil((float)remaining);
            if (lightIndex >= 0 && lightIndex < leftLights.Length)
            {
                leftLights[lightIndex].DOColor(targetLightColor, 0.15f);
            }
            if (lightIndex >= 0 && lightIndex < rightLights.Length)
            {
                rightLights[lightIndex].DOColor(targetLightColor, 0.15f);
            }
        }
        
        SoundManager.Instance?.Play("RaceStart");
        countdownNumberTMP.text = "GO";
        countdownNumberTMP.rectTransform.DOScale(1.7f * Vector3.one, 0.2f).SetEase(Ease.OutBounce);
        
        yield return GameplayClock.WaitForSeconds(1f);

        if (currentSessionState == SessionState.Countdown)
        {
            SessionManager.Instance.SetState(SessionState.Racing);
        }

        countdownRoutine = null;
    }

    protected override void OnDisable()
    {
        StopCountdown();
        base.OnDisable();
    }

    private void StopCountdown()
    {
        if (countdownRoutine == null)
        {
            return;
        }

        StopCoroutine(countdownRoutine);
        countdownRoutine = null;
    }
}
