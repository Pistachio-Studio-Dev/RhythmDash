using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class TimeIndicatorUI : SessionBasedBehaviour
{
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private TMP_Text timeText;

    private bool inRacingSession = false;
    private double sessionStartTime;
    
    private void Awake()
    {
        RefreshVisibility();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        RefreshVisibility();
    }

    private void Update()
    {
        if (!ShouldBeVisible())
        {
            return;
        }
        
        if (!inRacingSession)
        {
            timeText.text = "0.00";
            return;
        }

        double elapsedTime = GameplayClock.TimeAsDouble - sessionStartTime;
        timeText.text = elapsedTime.ToString("F2");
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        RefreshVisibility();
        
        if (newState == SessionState.Racing)
        {
            inRacingSession = true;
            sessionStartTime = GameplayClock.TimeAsDouble;
        }
        else
        {
            inRacingSession = false;
        }
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        base.OnGameplayPauseStateChanged(paused);
        RefreshVisibility();
    }

    private void RefreshVisibility()
    {
        cardRoot?.gameObject.SetActive(ShouldBeVisible());
    }

    private bool ShouldBeVisible()
    {
        return PlayerCharacterController.Instance != null &&
               PlayerCharacterController.Instance.vehicle != null &&
               cardRoot != null &&
               !isGameplayPaused &&
                currentSessionState == SessionState.Racing;
    }
}
