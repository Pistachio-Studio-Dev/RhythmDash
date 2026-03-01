// [ Written by Jiayi ]

using DG.Tweening;
using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    public void OnBtnResume()
    {
        PauseManager.Instance.Resume();
    }

    public void OnBtnMainMenu()
    {
        LevelManager.Instance.MainMenu();
        SessionManager.Instance?.EndSession();
    }
}
