// [ Written by Jiayi ]

using UnityEngine;

public class AudioReactiveTranslation : MonoBehaviour
{
    [SerializeField] private Vector3 translation = Vector3.up;

    [SerializeField] private bool useGlobalCoordinates = true;
    [SerializeField] private float responseCurve = 2f;

    private Vector3 position;

    private void Start()
    {
        position = useGlobalCoordinates ? transform.position : transform.localPosition;
    }

    private void Update()
    {
        var rawAmplitude = AudioAnalyzer.Instance.NormalizedValue;
        var amplitude = Mathf.Pow(rawAmplitude, responseCurve);
        if (useGlobalCoordinates)
            transform.position = position + translation * amplitude;
        else
            transform.localPosition = position + translation * amplitude;
    }

    private void OnDisable()
    {
        if (useGlobalCoordinates)
            transform.position = position;
        else
            transform.localPosition = position;
    }
}