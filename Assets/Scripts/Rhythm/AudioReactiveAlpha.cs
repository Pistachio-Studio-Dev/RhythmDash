// [ Written by Jiayi ]

using UnityEngine;
using UnityEngine.UI;

public class AudioReactiveAlpha : MonoBehaviour
{
    [SerializeField] private float responseCurve = 2f;
    [SerializeField] private float threshold = 0.3f;
    [SerializeField] private float baseValue = 0.3f;

    private Image image;

    private void Start()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        if (image == null) return;
        var rawAmplitude = AudioAnalyzer.Instance.NormalizedValue;
        var amplitude = Mathf.Pow(rawAmplitude, responseCurve);
        float alpha = Mathf.Min(1.0f, amplitude / threshold);
        image.color = Color.Lerp(new Color(1, 1, 1, baseValue), Color.white, alpha);
    }

    private void OnDisable()
    {
        image.color = Color.white;
    }
}