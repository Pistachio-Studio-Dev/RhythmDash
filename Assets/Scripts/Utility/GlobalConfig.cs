// [ Written by Jiayi ]

using EasyTransition;
using UnityEngine;

[CreateAssetMenu(fileName = "GlobalConfig", menuName = "Config/GlobalConfig")]
public class GlobalConfig : ScriptableObject
{
    private static GlobalConfig _config;

    public string DiscordTelemetryServer;
    
    public string RuntimeSettingsPath;

    public string ScenePath;
    public string MusicPath;

    public string InitScene;
    public string MainMenuScene;
    public string[] LevelSceneList;

    public float MusicDelayDuration = 2.0f;
    public float BeatDelayDuration = 2.0f;
    public float NotificationDuration = 5.0f;
    public float BeatIndicatorTimeOffset = 0.0f;
    public float BeatVibrationDuration = 0.08f;
    public float BeatVibrationStrengthLow = 0.35f;
    public float BeatVibrationStrengthHigh = 0.35f;
    public float DualSenseBeatVibrationDuration = 0.08f;
    public float DualSenseBeatVibrationStrengthLow = 0.35f;
    public float DualSenseBeatVibrationStrengthHigh = 0.35f;
    
    public int LapsPerSession = 3;

    public float BeatIndicatorAlpha = 0.7f;
    
    public float FastSpeedThreshold = 25f;
    public float MediumSpeedThreshold = 15f;
    
    public TransitionSettings SceneTransitionSettings;
    public TransitionSettings PauseTransitionSettings;
    public float SceneTransitionDelayDuration = 0.0f;
    public float PauseTransitionDelayDuration = 0.0f;
    
    public Color SlowAccentColor = new Color(0.9f, 0.22f, 0.18f, 1f);
    public Color MediumAccentColor = new Color(1f, 0.58f, 0.16f, 1f);
    public Color FastAccentColor = new Color(0.23f, 0.84f, 0.42f, 1f);

    public string GateObjectName;
    public string CheckpointName;
    
    public float PostProcessBlendSpeed = 10.0f;
    public float PostProcessDistortionMaxIntensity = 0.5f;
    public float PostProcessChromaticAberrationMaxIntensity = 0.5f;
    
    public float PowerUpStarDuration = 2.0f;
    public float PowerUpWingBoostDuration = 3.0f;
    public float PowerUpWingBoostSpeedMultiplier = 1.5f;
    public float PowerUpStunDuration = 2.0f;

    public static GlobalConfig Instance
    {
        get
        {
            if (_config == null)
            {
                _config = Resources.Load<GlobalConfig>("GlobalConfig");

                if (_config == null) Debug.LogError("GlobalConfig not found in Resources folder!");
            }

            return _config;
        }
    }
}
