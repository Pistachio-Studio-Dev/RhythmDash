// [ Written by Jiayi, Modified by Lindsey ]

using System.Collections.Generic;
using DG.Tweening;
using Event;
using UnityEngine;
using UnityEngine.UI;

public class BeatIndicator : SessionBasedBehaviour
{
    public GameObject Root;
    public GameObject Target;
    public GameObject Left;
    public GameObject Right;
    public GameObject Background;

    public IndicatorSettings settings;

    private Vector2 targetPosition;
    private Image targetImage;
    private Image backgroundImage;
    private Color originalColor;
    private Color originalBackgroundColor;
    private readonly List<GameObject> activeBeats = new();
    private bool beatEventsRegistered;

    private void Awake()
    {
        targetPosition = Target.GetComponent<RectTransform>().anchoredPosition;
        targetImage = Target.GetComponent<Image>();
        backgroundImage = Background.GetComponent<Image>();
        originalColor = targetImage.color;
        originalBackgroundColor = backgroundImage.color;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshIndicatorState();
    }

    protected override void OnDisable()
    {
        HideIndicatorImmediate();
        base.OnDisable();
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        RefreshIndicatorState();
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        RefreshIndicatorState();
    }

    private void RefreshIndicatorState()
    {
        bool shouldShow = currentSessionState == SessionState.Racing && !isGameplayPaused;

        if (Root != null)
        {
            Root.SetActive(shouldShow);
        }

        SetBeatEventsRegistered(shouldShow);

        if (!shouldShow)
        {
            ClearIndicatorArtifacts();
        }
    }

    private void OnSuccessHit()
    {
        DOTween.Sequence()
            .Append(targetImage.DOColor(settings.successColor, settings.fadeInDuration))
            .Append(targetImage.DOColor(originalColor, settings.fadeOutDuration));
        
        DOTween.Sequence()
            .Append(backgroundImage.DOColor(settings.successBackgroundColor, settings.fadeInDuration))
            .Append(backgroundImage.DOColor(originalBackgroundColor, settings.fadeOutDuration));
    }

    private void OnFailHit()
    {
        DOTween.Sequence()
            .Append(targetImage.DOColor(settings.failureColor, settings.fadeInDuration))
            .Append(targetImage.DOColor(originalColor, settings.fadeOutDuration));
        
        DOTween.Sequence()
            .Append(backgroundImage.DOColor(settings.failureBackgroundColor, settings.fadeInDuration))
            .Append(backgroundImage.DOColor(originalBackgroundColor, settings.fadeOutDuration));
    }

    private void OnAheadBeat()
    {
        var delay = GlobalConfig.Instance.MusicDelayDuration + GlobalConfig.Instance.BeatIndicatorTimeOffset;
        var fade = delay * 0.5f;

        var l = Instantiate(Left, Left.transform.parent);
        activeBeats.Add(l);
        l.SetActive(true);
        l.GetComponent<Image>().DOFade(GlobalConfig.Instance.BeatIndicatorAlpha, fade);
        l.GetComponent<RectTransform>().DOAnchorPos(targetPosition, delay)
            .SetEase(Ease.Linear)
            .OnComplete(() => DestroyBeatItem(l));

        var r = Instantiate(Right, Right.transform.parent);
        activeBeats.Add(r);
        r.SetActive(true);
        r.GetComponent<Image>().DOFade(GlobalConfig.Instance.BeatIndicatorAlpha, fade);
        r.GetComponent<RectTransform>().DOAnchorPos(targetPosition, delay)
            .SetEase(Ease.Linear)
            .OnComplete(() => DestroyBeatItem(r));
    }

    private void HideIndicatorImmediate()
    {
        if (Root != null)
        {
            Root.SetActive(false);
        }

        SetBeatEventsRegistered(false);
        ClearIndicatorArtifacts();
    }

    private void ClearIndicatorArtifacts()
    {
        foreach (var item in activeBeats)
        {
            Destroy(item);
        }
        activeBeats.Clear();
    }

    private void DestroyBeatItem(GameObject item)
    {
        activeBeats.Remove(item);
        Destroy(item);
    }
    
    private void SetBeatEventsRegistered(bool shouldRegister)
    {
        if (beatEventsRegistered == shouldRegister)
        {
            return;
        }

        if (shouldRegister)
        {
            EventManager.StartListening<BeatAhead>(OnAheadBeat);
            EventManager.StartListening<BeatHit>(OnSuccessHit);
            EventManager.StartListening<BeatHitFailed>(OnFailHit);
        }
        else
        {
            EventManager.StopListening<BeatAhead>(OnAheadBeat);
            EventManager.StopListening<BeatHit>(OnSuccessHit);
            EventManager.StopListening<BeatHitFailed>(OnFailHit);
        }

        beatEventsRegistered = shouldRegister;
    }
}
