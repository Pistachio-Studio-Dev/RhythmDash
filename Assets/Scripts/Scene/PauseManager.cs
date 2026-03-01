// [ Written by Jiayi ]

using EasyTransition;
using Event;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance  { get; private set; }

    public bool IsPaused => PauseMenu != null && PauseMenu.activeInHierarchy;
    public bool IsGameplayPaused { get; private set; }
    
    public GameObject PauseMenu;
    
    private bool inProgress = false;
    private double totalPausedDuration;
    private double gameplayPauseStartedAt = -1d;

    public void Pause()
    {
        if (PauseMenu == null)
        {
            Debug.LogError("PauseMenu is null");
            return;
        }

        if (IsPaused || inProgress)
        {
            Debug.LogWarning("Already paused or in progress, skipping");
            return;
        }
        
        inProgress = true;
        TransitionManager.Instance().onTransitionCutPointReached += PauseReachCallback;
        TransitionManager.Instance().onTransitionEnd += PauseFinishCallback;
        TransitionManager.Instance().Transition(GlobalConfig.Instance.PauseTransitionSettings, GlobalConfig.Instance.PauseTransitionDelayDuration);
    }

    public void Resume()
    {
        if (!IsPaused || inProgress)
        {
            Debug.LogWarning("Already running or in progress, skipping");
            return;
        }

        inProgress = true;
        TransitionManager.Instance().onTransitionCutPointReached += ResumeReachCallback;
        TransitionManager.Instance().onTransitionEnd += ResumeFinishCallback;
        TransitionManager.Instance().Transition(GlobalConfig.Instance.PauseTransitionSettings, GlobalConfig.Instance.PauseTransitionDelayDuration);
    }

    private void PauseReachCallback()
    {
        TransitionManager.Instance().onTransitionCutPointReached -= PauseReachCallback;
        PauseMenu.SetActive(true);
        SetGameplayPaused(true);
        Debug.Log("Paused");
    }

    private void PauseFinishCallback()
    {
        TransitionManager.Instance().onTransitionEnd -= PauseFinishCallback;
        inProgress = false;
    }
    
    private void ResumeReachCallback()
    {
        TransitionManager.Instance().onTransitionCutPointReached -= ResumeReachCallback;
        PauseMenu.SetActive(false);
        SetGameplayPaused(false);
        Debug.Log("Resumed");
    }
    
    private void ResumeFinishCallback()
    {
        TransitionManager.Instance().onTransitionEnd -= ResumeFinishCallback;
        inProgress = false;
    }
    
    private void OnLevelLoaded(string level)
    {
        inProgress = false;
        PauseMenu?.SetActive(false);
        SetGameplayPaused(false);
    }

    private void OnEnable()
    {
        EventManager.StartListening<LevelLoaded, string>(OnLevelLoaded);
    }

    private void OnDisable()
    {
        EventManager.StopListening<LevelLoaded, string>(OnLevelLoaded);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing PauseManager Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("PauseManager already exists, skipping initialization.");
            Destroy(gameObject);
        }

        if (PauseMenu == null)
        {
            Debug.Log("PauseMenu is null");
        }


        PauseMenu?.SetActive(false);
    }

    internal double CurrentGameplayTimeAsDouble
    {
        get
        {
            double pausedDuration = totalPausedDuration;
            if (IsGameplayPaused && gameplayPauseStartedAt >= 0d)
            {
                pausedDuration += Time.timeAsDouble - gameplayPauseStartedAt;
            }

            return Time.timeAsDouble - pausedDuration;
        }
    }

    private void SetGameplayPaused(bool paused)
    {
        if (paused)
        {
            if (IsGameplayPaused)
            {
                return;
            }

            gameplayPauseStartedAt = Time.timeAsDouble;
            IsGameplayPaused = true;
            
            EventManager.TriggerEvent<Pause>();

            return;
        }

        if (!IsGameplayPaused)
        {
            return;
        }

        if (gameplayPauseStartedAt >= 0d)
        {
            totalPausedDuration += Time.timeAsDouble - gameplayPauseStartedAt;
        }

        IsGameplayPaused = false;
        gameplayPauseStartedAt = -1d;
        
        EventManager.TriggerEvent<Resume>();
    }
}
