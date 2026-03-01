// [ Written by Jiayi ]

using UnityEngine;

public class AudioAnalyzer : MonoBehaviour
{
    [SerializeField] private EqualizerSettings equalizer;

    private float[] aheadSamples;
    private float aheadValueHighest;

    private float[] samples;
    private float valueHighest;
    public static AudioAnalyzer Instance { get; private set; }

    public float FrequencyValue { get; private set; }

    public float SmoothedValue { get; private set; }

    public float NormalizedValue { get; private set; }

    public float AheadFrequencyValue { get; private set; }

    public float AheadSmoothedValue { get; private set; }

    public float AheadNormalizedValue { get; private set; }

    private void Awake()
    {
        if (equalizer == null) Debug.LogError("AudioAnalyzer: AudioAnalyzer not assigned");

        if (Instance == null)
        {
            Debug.Log("Initializing Audio Analyzer Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("Audio Analyzer already exists, skipping initialization.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        samples = new float[equalizer.sampleCount];
        aheadSamples = new float[equalizer.sampleCount];
    }

    private void Update()
    {
        MusicPlayer.Instance.AudioSource.GetSpectrumData(samples, 0, equalizer.fftWindow);
        FrequencyValue = ComputeFrequencyValue(samples);
        SmoothedValue = ComputeSmoothValue(FrequencyValue, SmoothedValue);
        NormalizedValue = ComputeNormalizeValue(SmoothedValue, valueHighest);
        valueHighest = Mathf.Max(valueHighest, SmoothedValue);

        MusicPlayer.Instance.AheadAudioSource.GetSpectrumData(aheadSamples, 0, equalizer.fftWindow);
        AheadFrequencyValue = ComputeFrequencyValue(aheadSamples);
        AheadSmoothedValue = ComputeSmoothValue(AheadFrequencyValue, AheadSmoothedValue);
        AheadNormalizedValue = ComputeNormalizeValue(AheadSmoothedValue, aheadValueHighest);
        aheadValueHighest = Mathf.Max(aheadValueHighest, AheadSmoothedValue);
    }

    private float ComputeFrequencyValue(float[] samples)
    {
        var sampleIndex = 0;
        var totalValue = 0f;

        for (var band = 0; band < 8; band++)
        {
            var sampleCountForBand = (int)Mathf.Pow(2, band + 1);
            var sum = 0f;

            for (var s = 0; s < sampleCountForBand && sampleIndex < samples.Length; s++, sampleIndex++)
                sum += samples[sampleIndex];

            var bandAvg = sum / sampleCountForBand;
            var weight = equalizer.BandWeights.Length > band ? equalizer.BandWeights[band] : 1f;
            totalValue += bandAvg * weight;
        }

        return totalValue;
    }

    private float ComputeSmoothValue(float value, float currentSmoothed)
    {
        if (value > currentSmoothed) return Mathf.Lerp(currentSmoothed, value, Time.deltaTime * equalizer.riseSpeed);

        return Mathf.Lerp(currentSmoothed, value, Time.deltaTime * equalizer.fallSpeed);
    }

    private float ComputeNormalizeValue(float value, float highest)
    {
        if (highest > 0f) return value / highest;

        return 0f;
    }
}