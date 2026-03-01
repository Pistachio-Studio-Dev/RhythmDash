// [ Written by Jiayi ]

using UnityEngine;

[CreateAssetMenu(fileName = "BeatSettings", menuName = "Rhythm/Beat Settings")]
public class BeatSettings : ScriptableObject
{
    public float beatThreshold = 1.08f;

    public float cooldownTime = 0.25f;
    
    public float historyTimeWindow = 0.2f;

    public float inputWindowDuration = 0.5f;

    public float inputWindowOffset = -0.25f;
}