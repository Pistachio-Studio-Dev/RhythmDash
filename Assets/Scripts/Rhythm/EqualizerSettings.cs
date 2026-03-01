// [ Written by Jiayi ]

using UnityEngine;

[CreateAssetMenu(fileName = "EqualizerSettings", menuName = "Rhythm/Equalizer Settings")]
public class EqualizerSettings : ScriptableObject
{
    public float[] BandWeights = new float[8] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

    public int sampleCount = 512;

    public FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    public float fallSpeed = 5f;

    public float riseSpeed = 20f;
}