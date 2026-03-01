// [ Written by Jiayi ]

using System;
using System.IO;
using UnityEngine;
using DG.Tweening;
using EasyTransition;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainRoot;
    [SerializeField] private GameObject levelRoot;
    
    private RectTransform mainRect;
    private RectTransform levelRect;

    private Tween tween;
    
    private const float duration = 0.8f;
    private const Ease ease = Ease.InOutQuart;

    private void Awake()
    {
        mainRect = mainRoot.GetComponent<RectTransform>();
        levelRect = levelRoot.GetComponent<RectTransform>();

        if (mainRect == null || levelRect == null)
        {
            Debug.LogError("MainMenuUI: Missing required components on mainRoot or levelRoot.");
        }

        DOTween.Sequence()
            .AppendInterval(0.1f)
            .AppendCallback(() => levelRoot.SetActive(false));
    }

    public void OnBtnStart()
    {
        tween?.Kill();
        tween = DOTween.Sequence()
            .AppendCallback(() => levelRoot.gameObject.SetActive(true))
            .Append(levelRect.DOAnchorMin(new Vector2(0, 0), duration).SetEase(ease))
            .Join(levelRect.DOAnchorMax(new Vector2(1, 1), duration).SetEase(ease))
            .Join(mainRect.DOAnchorMin(new Vector2(0, 1), duration).SetEase(ease))
            .Join(mainRect.DOAnchorMax(new Vector2(1, 2), duration).SetEase(ease))
            .AppendCallback(() => mainRoot.gameObject.SetActive(false));
    }

    public void OnBtnBack()
    {
        tween?.Kill();
        tween = DOTween.Sequence()
            .AppendCallback(() => mainRoot.gameObject.SetActive(true))
            .Append(mainRect.DOAnchorMin(new Vector2(0, 0), duration).SetEase(ease))
            .Join(mainRect.DOAnchorMax(new Vector2(1, 1), duration).SetEase(ease))
            .Join(levelRect.DOAnchorMin(new Vector2(0, -1), duration).SetEase(ease))
            .Join(levelRect.DOAnchorMax(new Vector2(1, 0), duration).SetEase(ease))
            .AppendCallback(() => levelRoot.gameObject.SetActive(false));
    }

    public void OnBtnQuit()
    {
#if !UNITY_EDITOR
        TransitionManager.Instance().onTransitionCutPointReached += () => { LevelManager.Instance.Quit(); };
#endif
        TransitionManager.Instance().Transition(GlobalConfig.Instance.SceneTransitionSettings, GlobalConfig.Instance.SceneTransitionDelayDuration);
    }

    public void OnBtnMusic()
    {
        string musicPath = GlobalConfig.Instance.MusicPath;
        musicPath = Path.Combine(Application.streamingAssetsPath, musicPath);

        if (!Directory.Exists(musicPath))
        {
            Debug.LogError($"Music directory not found: {musicPath}");
        }
        
        Application.OpenURL($"file://{musicPath}");
        NotificationCenter.Instance?.ShowNotification("Open Music Folder\nYou need to restart the game after adding new music.");
    }
    
    public void OnBtnTutorialLevel()
    {
        LevelManager.Instance.SetLevel(0);
        SessionManager.Instance?.StartSession();
    }
    
    public void OnBtnMainLevel()
    {
        LevelManager.Instance.SetLevel(1);
        SessionManager.Instance?.StartSession();
    }
}
