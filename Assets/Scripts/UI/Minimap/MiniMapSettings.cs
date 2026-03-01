using UnityEngine;

[CreateAssetMenu(fileName = "MiniMapSettings", menuName = "UI/MiniMap Settings")]
public class MiniMapSettings : ScriptableObject
{
    [Header("Layout")]
    public Vector2 safeAreaMargin = new Vector2(24f, 24f);
    [Range(0.12f, 0.5f)] public float panelShortEdgeFactor = 0.28f;
    public float minPanelHeight = 180f;
    public float maxPanelHeight = 320f;
    [Range(0.35f, 1f)] public float panelAspect = 0.68f;
    public float cardPadding = 18f;

    [Header("Track")]
    [Range(0.02f, 0.4f)] public float trackPaddingRatio = 0.12f;
    public float trackOuterWidth = 16f;
    public float trackInnerWidth = 9f;
    public float checkpointRadius = 6f;
    public float nextCheckpointRadius = 8f;
    public float finishBarLength = 26f;
    public float finishBarWidth = 8f;
    public float radarWorldRadius = 72f;
    public float radarEdgePadding = 12f;
    [Range(0.1f, 1f)] public float trackFadeStartNormalized = 0.58f;
    [Range(0f, 1f)] public float trackEdgeAlpha = 0.04f;
    [Range(0.1f, 1f)] public float aiFadeStartNormalized = 0.72f;
    [Range(0f, 1f)] public float aiEdgeAlpha = 0f;
    [Range(0f, 1f)] public float pointOfInterestEdgeAlpha = 0.12f;

    [Header("Markers")]
    public float playerMarkerSize = 20f;
    public float aiMarkerSize = 14f;
    public float markerSmoothing = 16f;
    [Range(0f, 1f)] public float finishedMarkerAlpha = 0.35f;

    [Header("Colors")]
    public Color trackOuterColor = new Color(0.08f, 0.09f, 0.12f, 0.95f);
    public Color trackInnerColor = new Color(0.94f, 0.96f, 0.99f, 0.98f);
    public Color checkpointColor = new Color(1f, 0.72f, 0.22f, 1f);
    public Color completedCheckpointColor = new Color(0.26f, 0.90f, 0.52f, 1f);
    public Color nextCheckpointColor = new Color(0.20f, 0.82f, 1f, 1f);
    public Color playerMarkerColor = new Color(1f, 0.30f, 0.24f, 1f);
    public Color playerMarkerOutlineColor = Color.white;
    public Color playerMarkerGlowColor = new Color(1f, 0.84f, 0.78f, 0.55f);
    public Color aiMarkerColor = new Color(1f, 0.86f, 0.24f, 1f);
    public Color aiMarkerOutlineColor = new Color(0.08f, 0.08f, 0.10f, 0.95f);
    public Color startFinishPrimaryColor = Color.white;
    public Color startFinishSecondaryColor = new Color(0.08f, 0.08f, 0.08f, 1f);

    [Header("Generation")]
    public bool useManualRotationOverride = false;
    [Range(-180f, 180f)] public float manualRotationOffsetDegrees = 0f;

    [Header("Visibility")]
    public bool showInCountdown = true;
    public bool showInRacing = true;
    public bool showInFinished = true;
}
