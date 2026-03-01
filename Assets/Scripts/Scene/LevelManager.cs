// [ Written by Jiayi ]

using System.IO;
using EasyTransition;
using Event;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance  { get; private set; }
    
    public string CurrentLevelName => SceneManager.GetActiveScene().name;
    public int CurrentLevelIndex = 0;
    public bool InTransition { get; set; } = false;

    private string currentSceneName;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing LevelManager Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("LevelManager already exists, skipping initialization.");
            Destroy(gameObject);
        }
    }
    
    public void MainMenu()
    {
        ForceLoadScene(GlobalConfig.Instance.MainMenuScene);
    }
    
    public void SetLevel(int index)
    {
        if (index < 0 || index >= GlobalConfig.Instance.LevelSceneList.Length)
        {
            Debug.LogError($"Invalid level index: {index}");
            return;
        }
        
        CurrentLevelIndex = index;
        CurrentLevel();
    }

    public void CurrentLevel()
    {
        ForceLoadScene(GlobalConfig.Instance.LevelSceneList[CurrentLevelIndex]);
    }

    public void NextLevel()
    {
        CurrentLevelIndex++;
        if (CurrentLevelIndex >= GlobalConfig.Instance.LevelSceneList.Length)
        {
            CurrentLevelIndex = 0;
            MainMenu();
            return;
        }

        CurrentLevel();
    }

    public void ResetLevel()
    {
        CurrentLevelIndex = 0;
    }
    
    public void ForceLoadScene(string sceneName)
    {
        TransitionManager.Instance().onTransitionCutPointReached += OnSceneChanged;
        TransitionManager.Instance().onTransitionEnd += OnTransitionEnd;
        TransitionManager.Instance().Transition(sceneName, 
            GlobalConfig.Instance.SceneTransitionSettings, 
            GlobalConfig.Instance.SceneTransitionDelayDuration
        );
        currentSceneName = sceneName;
        InTransition = true;
    }

    public void ForceLoadScene(int index)
    {
        TransitionManager.Instance().onTransitionCutPointReached += OnSceneChanged;
        TransitionManager.Instance().onTransitionEnd += OnTransitionEnd;
        TransitionManager.Instance().Transition(index, 
            GlobalConfig.Instance.SceneTransitionSettings, 
            GlobalConfig.Instance.SceneTransitionDelayDuration
        );
        currentSceneName = SceneManager.GetSceneAt(index).name;
        InTransition = true;
    }

    private void OnSceneChanged()
    {
        TransitionManager.Instance().onTransitionCutPointReached -= OnSceneChanged;
        Debug.Log($"Scene {currentSceneName} has been loaded");
        EventManager.TriggerEvent<LevelLoaded, string>(currentSceneName);
    }

    private void OnTransitionEnd()
    {
        TransitionManager.Instance().onTransitionEnd -= OnTransitionEnd;
        Debug.Log($"Scene {currentSceneName} transition end");
        InTransition = false;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void PrintAllScenes()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;

        string output = "All Scenes:\n";

        for (var i = 0; i < sceneCount; i++)
        {
            var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneName = Path.GetFileNameWithoutExtension(scenePath);

            output += $"Scene {i}: {sceneName}\n";
        }
        
        Debug.Log(output);
    }
}
