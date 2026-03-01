// [ Written by Jiayi ]

using System;
using System.Collections;
using DG.Tweening;
using Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicNotification : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.StartListening<MusicStarted, string>(OnMusicStarted);
    }

    private void OnDisable()
    {
        EventManager.StopListening<MusicStarted, string>(OnMusicStarted);
    }

    private void OnMusicStarted(string name)
    {
        string notification = $"Now Playing:\n{name}";
        NotificationCenter.Instance?.ShowNotification(notification);
    }
}
