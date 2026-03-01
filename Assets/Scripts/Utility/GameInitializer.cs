// [ Written by Jiayi ]

using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    public GameObject DebugConsole;
    
    private void Start()
    {
        Init();
    }

    private void Init()
    {
        RuntimeSettings.Load();
        
#if !UNITY_EDITOR
        if (DebugConsole != null && !RuntimeSettings.Instance.EnableConsole)
        {
            DebugConsole.SetActive(false);
        }
#endif
        
        LevelManager.Instance.MainMenu();
    }
}