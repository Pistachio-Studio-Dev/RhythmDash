using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
public class MiniMapTrackGraphic : MaskableGraphic
{
    private const float SegmentCullThreshold = 1.15f;

    private MiniMapSettings settings;
    private MiniMapTrackSourceResult trackData;
    private int previousCheckpointIndex = -1;
    private int nextCheckpointIndex = -1;
    private bool playerFinished;
    private bool hasRadarContext;
    private Vector2 radarOrigin;
    private Vector2 radarRight;
    private Vector2 radarForward;

    public void ApplySettings(MiniMapSettings minimapSettings)
    {
        settings = minimapSettings;
        SetVerticesDirty();
    }

    public void SetTrackData(MiniMapTrackSourceResult data)
    {
        trackData = data;
        SetVerticesDirty();
    }

    public void SetCheckpointState(int previousIndex, int nextIndex, bool hasFinished)
    {
        previousCheckpointIndex = previousIndex;
        nextCheckpointIndex = nextIndex;
        playerFinished = hasFinished;
        SetVerticesDirty();
    }

    public void SetRadarContext(Vector3 playerWorldPosition, Vector3 playerWorldForward)
    {
        hasRadarContext = MiniMapProjection.TryBuildRadarBasis(
            playerWorldPosition,
            playerWorldForward,
            out radarOrigin,
            out radarRight,
            out radarForward);
        SetVerticesDirty();
    }

    public void ClearRadarContext()
    {
        if (!hasRadarContext)
            return;

        hasRadarContext = false;
        SetVerticesDirty();
    }

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (settings == null || trackData == null || !trackData.IsValid || !hasRadarContext)
            return;

        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        Vector2 center = rect.center;
        float pixelRadius = GetRadarPixelRadius(rect);
        if (pixelRadius <= 0f)
            return;

        DrawTrack(vertexHelper, center, pixelRadius);
        DrawStartGate(vertexHelper, center, pixelRadius);
        DrawCheckpoints(vertexHelper, center, pixelRadius);
    }

    private void DrawTrack(VertexHelper vertexHelper, Vector2 center, float pixelRadius)
    {
        if (trackData.WorldTrackPoints == null || trackData.WorldTrackPoints.Count < 2)
            return;

        if (trackData.SegmentPointCounts != null && trackData.SegmentPointCounts.Count > 0)
        {
            int segmentStartIndex = 0;
            for (int segmentIndex = 0; segmentIndex < trackData.SegmentPointCounts.Count; segmentIndex++)
            {
                int pointCount = trackData.SegmentPointCounts[segmentIndex];
                DrawTrackSegment(vertexHelper, center, pixelRadius, segmentStartIndex, pointCount);
                segmentStartIndex += pointCount;
            }

            return;
        }

        DrawTrackSegment(vertexHelper, center, pixelRadius, 0, trackData.WorldTrackPoints.Count);
    }

    private void DrawTrackSegment(VertexHelper vertexHelper, Vector2 center, float pixelRadius, int startIndex, int pointCount)
    {
        if (pointCount < 2)
            return;

        int lineCount = pointCount - 1;
        for (int lineIndex = 0; lineIndex < lineCount; lineIndex++)
        {
            int worldStartIndex = startIndex + lineIndex;
            int worldEndIndex = startIndex + lineIndex + 1;

            if (worldStartIndex < 0 || worldEndIndex < 0 ||
                worldStartIndex >= trackData.WorldTrackPoints.Count ||
                worldEndIndex >= trackData.WorldTrackPoints.Count)
            {
                continue;
            }

            Vector3 startWorld = trackData.WorldTrackPoints[worldStartIndex];
            Vector3 endWorld = trackData.WorldTrackPoints[worldEndIndex];

            Vector2 startPoint = ProjectToRadar(center, pixelRadius, startWorld, out float startDistance01);
            Vector2 endPoint = ProjectToRadar(center, pixelRadius, endWorld, out float endDistance01);

            if (startDistance01 > SegmentCullThreshold && endDistance01 > SegmentCullThreshold)
                continue;

            float outerStartAlpha = EvaluateTrackAlpha(startDistance01);
            float outerEndAlpha = EvaluateTrackAlpha(endDistance01);
            if (outerStartAlpha <= 0.001f && outerEndAlpha <= 0.001f)
                continue;

            Color32 outerStart = ApplyAlpha(settings.trackOuterColor, outerStartAlpha);
            Color32 outerEnd = ApplyAlpha(settings.trackOuterColor, outerEndAlpha);
            Color32 innerStart = ApplyAlpha(settings.trackInnerColor, outerStartAlpha);
            Color32 innerEnd = ApplyAlpha(settings.trackInnerColor, outerEndAlpha);

            MiniMapMeshBuilder.AddLineSegmentGradient(vertexHelper, startPoint, endPoint, settings.trackOuterWidth, outerStart, outerEnd);
            MiniMapMeshBuilder.AddLineSegmentGradient(vertexHelper, startPoint, endPoint, settings.trackInnerWidth, innerStart, innerEnd);
        }
    }

    private void DrawStartGate(VertexHelper vertexHelper, Vector2 center, float pixelRadius)
    {
        if (!trackData.StartGate.isValid)
            return;

        Vector2 gateCenter = ProjectToRadar(center, pixelRadius, trackData.StartGate.worldPosition, out float distance01);
        float alpha = EvaluatePointOfInterestAlpha(distance01);
        if (alpha <= 0.001f || distance01 > SegmentCullThreshold)
            return;

        Vector2 tangent = MiniMapProjection.ProjectRadarDirection(radarRight, radarForward, trackData.StartGate.worldTangent);
        if (tangent.sqrMagnitude <= 0.0001f)
        {
            tangent = Vector2.up;
        }

        Vector2 perpendicular = new Vector2(-tangent.y, tangent.x);
        float stripeLength = settings.finishBarLength / 6f;

        for (int i = 0; i < 6; i++)
        {
            float startOffset = -settings.finishBarLength * 0.5f + stripeLength * i;
            float endOffset = startOffset + stripeLength;
            Vector2 start = gateCenter + perpendicular * startOffset;
            Vector2 end = gateCenter + perpendicular * endOffset;
            Vector2 normal = tangent * (settings.finishBarWidth * 0.5f);
            Color stripeBaseColor = (i % 2 == 0) ? settings.startFinishPrimaryColor : settings.startFinishSecondaryColor;

            MiniMapMeshBuilder.AddQuad(
                vertexHelper,
                start - normal,
                start + normal,
                end + normal,
                end - normal,
                ApplyAlpha(stripeBaseColor, alpha));
        }
    }

    private void DrawCheckpoints(VertexHelper vertexHelper, Vector2 center, float pixelRadius)
    {
        if (trackData.Checkpoints == null)
            return;

        for (int i = 0; i < trackData.Checkpoints.Count; i++)
        {
            MiniMapCheckpointData checkpoint = trackData.Checkpoints[i];
            Vector2 checkpointPoint = ProjectToRadar(center, pixelRadius, checkpoint.worldPosition, out float distance01);
            float alpha = EvaluatePointOfInterestAlpha(distance01);
            if (alpha <= 0.001f || distance01 > SegmentCullThreshold)
                continue;

            float radius = checkpoint.index == nextCheckpointIndex && !playerFinished
                ? settings.nextCheckpointRadius
                : settings.checkpointRadius;

            Color color = settings.checkpointColor;
            if (playerFinished || checkpoint.index == previousCheckpointIndex)
            {
                color = settings.completedCheckpointColor;
            }
            else if (checkpoint.index == nextCheckpointIndex)
            {
                color = settings.nextCheckpointColor;
            }

            MiniMapMeshBuilder.AddCircle(
                vertexHelper,
                checkpointPoint,
                radius,
                ApplyAlpha(color, alpha),
                14);
        }
    }

    private Vector2 ProjectToRadar(Vector2 center, float pixelRadius, Vector3 worldPosition, out float distance01)
    {
        Vector2 normalizedOffset = MiniMapProjection.ProjectRadarPoint(
            radarOrigin,
            radarRight,
            radarForward,
            worldPosition,
            settings.radarWorldRadius);

        distance01 = normalizedOffset.magnitude;
        return center + normalizedOffset * pixelRadius;
    }

    private float GetRadarPixelRadius(Rect rect)
    {
        return Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - settings.radarEdgePadding);
    }

    private float EvaluateTrackAlpha(float distance01)
    {
        if (distance01 <= settings.trackFadeStartNormalized)
            return 1f;

        float t = Mathf.InverseLerp(settings.trackFadeStartNormalized, 1f, distance01);
        return Mathf.Lerp(1f, settings.trackEdgeAlpha, t);
    }

    private float EvaluatePointOfInterestAlpha(float distance01)
    {
        if (distance01 <= settings.trackFadeStartNormalized)
            return 1f;

        float t = Mathf.InverseLerp(settings.trackFadeStartNormalized, 1f, distance01);
        return Mathf.Lerp(1f, settings.pointOfInterestEdgeAlpha, t);
    }

    private static Color32 ApplyAlpha(Color color, float alphaMultiplier)
    {
        color.a *= Mathf.Clamp01(alphaMultiplier);
        return color;
    }
}
