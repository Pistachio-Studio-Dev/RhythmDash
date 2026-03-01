// [ Written by Jiayi ]

using System;
using DG.Tweening;
using Event;
using UnityEngine;
using UnityEngine.UI;

public class BeatReactiveColor : MonoBehaviour
{
    public IndicatorSettings settings;
    public Color targetColor;
    public bool useRandomColor;
    public float saturation;
    
    private Image targetImage;
    
    private Color originalColor;

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        originalColor = targetImage.color;
    }

    private void OnEnable()
    {
        EventManager.StartListening<Beat>(OnBeat);
    }

    private void OnDisable()
    {
        EventManager.StopListening<Beat>(OnBeat);
    }

    private void OnBeat()
    {
        if (useRandomColor)
        {
            targetColor = Color.HSVToRGB(UnityEngine.Random.value, saturation, 1f);
        }
        DOTween.Sequence()
            .Append(targetImage.DOColor(targetColor, settings.fadeInDuration))
            .Append(targetImage.DOColor(originalColor, settings.fadeOutDuration));
    }
}
