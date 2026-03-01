// [ Written by Jiayi ]

using System;
using Event;
using UnityEngine;

public class ForcePlay : MonoBehaviour
{
    public string musicName;

    private void OnEnable()
    {
        EventManager.StartListening<MusicStarted, string>(OnMusicStarted);
    }

    private void OnDisable()
    {
        EventManager.StopListening<MusicStarted, string>(OnMusicStarted);
    }

    private void OnMusicStarted(string music)
    {
        if (!music.Contains(musicName))
        {
            MusicPlayer.Instance.Play(musicName);
        }
    }
}
