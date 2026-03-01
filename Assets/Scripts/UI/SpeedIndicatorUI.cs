using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SpeedIndicatorUI : SessionBasedBehaviour
{
    
    private const float MetersPerSecondToMilesPerHour = 2.2369363f;
    private const float AccentColorBlendSpeed = 3f;
    
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private Image accentBar;
    [SerializeField] private TMP_Text speedText;

    private int lastDisplayedSpeed = int.MinValue;

    private void Awake()
    {
        RefreshVisibility();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        lastDisplayedSpeed = int.MinValue;
        RefreshVisibility();
    }

    private void Start()
    {
        RefreshVisualsImmediate();
    }

    private void Update()
    {
        if (!ShouldBeVisible())
        {
            return;
        }

        UpdateSpeedText();
        UpdateAccentColor();
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        RefreshVisibility();
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

    private void RefreshVisualsImmediate()
    {
        lastDisplayedSpeed = int.MinValue;
        UpdateSpeedText();
        ApplyAccentColorImmediate();
    }

    private void UpdateSpeedText()
    {
        if (speedText == null)
        {
            return;
        }

        int speedMph = Mathf.RoundToInt(GetCurrentSpeedMph());
        if (speedMph == lastDisplayedSpeed)
        {
            return;
        }

        lastDisplayedSpeed = speedMph;
        speedText.text = speedMph.ToString();
    }

    private void UpdateAccentColor()
    {
        if (accentBar == null)
        {
            return;
        }

        Color targetColor = GetAccentTargetColor();
        float blend = 1f - Mathf.Exp(-AccentColorBlendSpeed * Time.unscaledDeltaTime);
        accentBar.color = Color.Lerp(accentBar.color, targetColor, blend);
    }

    private void ApplyAccentColorImmediate()
    {
        if (accentBar != null)
        {
            accentBar.color = GetAccentTargetColor();
        }
    }

    private float GetCurrentSpeedMph()
    {
        return PlayerCharacterController.Instance.vehicle == null
            ? 0f
            : Mathf.Max(0f, PlayerCharacterController.Instance.vehicle.CurrentSpeed * MetersPerSecondToMilesPerHour);
    }

    private Color GetAccentTargetColor()
    {
        float speedMph = GetCurrentSpeedMph();
        if (speedMph >= GlobalConfig.Instance.FastSpeedThreshold)
        {
            return GlobalConfig.Instance.FastAccentColor;
        }

        if (speedMph >= GlobalConfig.Instance.MediumSpeedThreshold)
        {
            return GlobalConfig.Instance.MediumAccentColor;
        }

        return GlobalConfig.Instance.SlowAccentColor;
    }
}
