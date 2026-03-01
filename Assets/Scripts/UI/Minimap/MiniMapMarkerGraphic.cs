using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasRenderer))]
public class MiniMapMarkerGraphic : MaskableGraphic
{
    [SerializeField] private bool isPlayer;
    [SerializeField] private float outlineThickness = 2f;
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private Color outlineColor = new Color(0.08f, 0.08f, 0.1f, 0.95f);
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private float alpha = 1f;
    [SerializeField] private float visibilityAlpha = 1f;

    public void Configure(bool playerMarker, MiniMapSettings settings)
    {
        isPlayer = playerMarker;
        fillColor = playerMarker ? settings.playerMarkerColor : settings.aiMarkerColor;
        outlineColor = playerMarker ? settings.playerMarkerOutlineColor : settings.aiMarkerOutlineColor;
        glowColor = playerMarker ? settings.playerMarkerGlowColor : new Color(settings.aiMarkerColor.r, settings.aiMarkerColor.g, settings.aiMarkerColor.b, 0.25f);
        alpha = 1f;
        visibilityAlpha = 1f;
        SetVerticesDirty();
    }

    public void SetFinishedState(bool finished, MiniMapSettings settings)
    {
        alpha = finished ? settings.finishedMarkerAlpha : 1f;
        SetVerticesDirty();
    }

    public void SetVisibilityAlpha(float newVisibilityAlpha)
    {
        visibilityAlpha = Mathf.Clamp01(newVisibilityAlpha);
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
        Rect rect = GetPixelAdjustedRect();

        if (rect.width <= 0f || rect.height <= 0f)
            return;

        Vector2 center = rect.center;
        float width = rect.width;
        float height = rect.height;

        float compositeAlpha = alpha * visibilityAlpha;
        Color32 adjustedFill = fillColor * compositeAlpha;
        Color32 adjustedOutline = outlineColor * compositeAlpha;
        Color32 adjustedGlow = glowColor * compositeAlpha;

        MiniMapMeshBuilder.AddCircle(vertexHelper, center, Mathf.Min(width, height) * (isPlayer ? 0.42f : 0.35f), adjustedGlow, 14);
        MiniMapMeshBuilder.AddFilledConvexPolygon(vertexHelper, BuildArrowPoints(center, width, height, 0f), adjustedOutline);
        MiniMapMeshBuilder.AddFilledConvexPolygon(
            vertexHelper,
            BuildArrowPoints(center, width - outlineThickness * 2f, height - outlineThickness * 2f, outlineThickness),
            adjustedFill);
    }

    private static List<Vector2> BuildArrowPoints(Vector2 center, float width, float height, float inset)
    {
        float halfWidth = Mathf.Max(2f, width * 0.5f - inset);
        float halfHeight = Mathf.Max(2f, height * 0.5f - inset);
        float shoulder = halfWidth * 0.52f;
        float tail = halfHeight * 0.50f;
        float mid = halfHeight * 0.08f;

        return new List<Vector2>
        {
            center + new Vector2(0f, halfHeight),
            center + new Vector2(halfWidth, -mid),
            center + new Vector2(shoulder, -mid),
            center + new Vector2(shoulder * 0.55f, -tail),
            center + new Vector2(-shoulder * 0.55f, -tail),
            center + new Vector2(-shoulder, -mid),
            center + new Vector2(-halfWidth, -mid)
        };
    }
}
