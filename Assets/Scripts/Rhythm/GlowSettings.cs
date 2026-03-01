// [ Written by Jiayi ]

using UnityEngine;

[CreateAssetMenu(fileName = "GlowSettings", menuName = "Rhythm/Glow Settings")]
public class GlowSettings : ScriptableObject
{
    public Shader glowShader;
    
    public Color glowColor = Color.white;

    [Range(0f, 0.5f)] public float edgeWidth = 0.12f;

    [Range(0.01f, 1f)] public float softness = 0.55f;

    public float maxIntensity = 1.4f;

    public float fadeInDuration = 0.04f;

    public float fadeOutDuration = 0.35f;

    public AnimationCurve fadeOutCurve = new(
        new Keyframe(0f, 1f, 0f, -0.5f),
        new Keyframe(0.4f, 0.45f, -1.2f, -1.8f),
        new Keyframe(1f, 0f, -0.8f, 0f)
    );
}