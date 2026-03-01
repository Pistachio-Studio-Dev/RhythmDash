// [ Written by Jiayi ]

using UnityEngine;

[CreateAssetMenu(fileName = "IndicatorSettings", menuName = "Rhythm/Indicator Settings")]
public class IndicatorSettings : ScriptableObject
{
    public Color successColor = Color.green;
    public Color failureColor = Color.red;
    public Color successBackgroundColor = Color.green;
    public Color failureBackgroundColor = Color.red;
    public float fadeInDuration = 0.04f;
    public float fadeOutDuration = 0.3f;
}