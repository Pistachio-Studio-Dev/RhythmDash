// [ Written by Jiayi ]

using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    private InputAction pauseAction;

    void Awake()
    {
        pauseAction = InputSystem.actions["Pause"];
        if (pauseAction == null) Debug.LogError("Pause action not found");
    }

    private void Update()
    {
        if (pauseAction == null || PauseManager.Instance == null)
        {
            return;
        }

        if (pauseAction.WasPerformedThisFrame())
        {
            if (PauseManager.Instance.IsPaused)
            {
                PauseManager.Instance.Resume();
            }
            else
            {
                PauseManager.Instance.Pause();
            }
        }
    }
}
