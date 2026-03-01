// [ Written by Jiayi ]

using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroMovie : MonoBehaviour
{
    public bool showOnce;
    public VideoPlayer videoPlayer;
    public RawImage videoImage;
    
    private static bool hasShown = false;

    private void Awake()
    {
        if (showOnce && hasShown)
        {
            return;
        }
        
        Debug.Log("IntroMovie Start");
        hasShown = true;
        
        try
        {
            videoImage.gameObject.SetActive(true);
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.Play();
        }
        catch(Exception exception)
        {
            Debug.LogError(exception);
            RemoveVideoImage();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        videoPlayer.loopPointReached -= OnVideoFinished;
        Debug.Log("IntroMovie Finish");
        vp.Stop();
        RemoveVideoImage();
    }

    private void RemoveVideoImage()
    {
        DOTween.Sequence()
            .Append(videoImage.DOFade(0, 0.5f))
            .AppendCallback(() => videoImage.gameObject.SetActive(false));
    }
}
