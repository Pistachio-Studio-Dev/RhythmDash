// [ Written by Jiayi ]

using System;
using UnityEngine;

public class AudioReactiveScale : MonoBehaviour
{
    [SerializeField] private Vector3 relativeScale = Vector3.one * 2f;

    [SerializeField] private float responseCurve = 2f;

    private Vector3 localScale;
    private Vector3 maxLocalScale;

    private void Update()
    {
        var rawAmplitude = AudioAnalyzer.Instance.NormalizedValue;
        var amplitude = Mathf.Pow(rawAmplitude, responseCurve);
        transform.localScale = Vector3.Lerp(localScale, maxLocalScale, amplitude);
    }

    private void OnEnable()
    {
        localScale = transform.localScale;
        maxLocalScale = Vector3.Scale(localScale, relativeScale);
    }

    private void OnDisable()
    {
        transform.localScale = localScale;
    }
}