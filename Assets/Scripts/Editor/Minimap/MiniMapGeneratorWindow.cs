using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniMapGeneratorWindow : EditorWindow
{
    private const float ScreenStrokeSampleDistance = 3f;
    private const float NormalizedStrokeMergeDistance = 0.0015f;
    private const float ResampleSpacing = 0.01f;
    private const int MinimumGeneratedPointCount = 12;
    private const int MaximumGeneratedPointCount = 256;
    private const int ChaikinIterations = 2;

    private readonly struct GeneratedStroke
    {
        public GeneratedStroke(List<Vector2> points)
        {
            Points = points ?? new List<Vector2>();
        }

        public List<Vector2> Points { get; }
        public bool IsValid => Points != null && Points.Count > 1;
    }

    private MiniMapSettings settings;
    private MiniMapTrackData trackData;
    private Texture2D previewTexture;
    private string loadedPreviewPath;
    private readonly List<Vector2> activeStrokePoints = new List<Vector2>();
    private bool isDrawingStroke;

    public static void Open()
    {
        MiniMapGeneratorWindow window = GetWindow<MiniMapGeneratorWindow>("MiniMap Generator");
        window.minSize = new Vector2(420f, 560f);
        window.ReloadAssets();
    }

    private void OnEnable()
    {
        ReloadAssets();
    }

    private void OnFocus()
    {
        ReloadPreviewTexture();
    }

    private void OnGUI()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        EnsureSceneAssetsLoaded(activeScene);

        EditorGUILayout.LabelField("Active Scene", activeScene.name, EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        settings = (MiniMapSettings)EditorGUILayout.ObjectField("Settings Asset", settings, typeof(MiniMapSettings), false);
        if (settings == null && GUILayout.Button("Create / Load Shared Settings"))
        {
            settings = MiniMapGenerator.GetOrCreateSettingsAsset();
        }

        trackData = (MiniMapTrackData)EditorGUILayout.ObjectField("Track Asset", trackData, typeof(MiniMapTrackData), false);
        if (trackData == null && GUILayout.Button("Create / Load Scene Track Data"))
        {
            trackData = MiniMapGenerator.GetOrCreateTrackDataAsset(activeScene.name);
            ReloadPreviewTexture();
        }

        EnsureTrackDataCollections();

        EditorGUILayout.Space(8f);
        DrawToolbar();
        DrawPrimaryAction();
        EditorGUILayout.Space(8f);

        EditorGUILayout.HelpBox(
            "Capture a top-down map first, then draw the route with one or more pencil strokes. " +
            "Each stroke is cleaned, smoothed, and baked as its own open segment so separate strokes can stay detached.",
            MessageType.Info);

        Rect previewRect = GUILayoutUtility.GetAspectRect(GetPreviewAspect(), GUILayout.ExpandWidth(true));
        DrawAuthoringPreview(previewRect);
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(trackData == null))
            {
                if (GUILayout.Button("Capture Top-Down Map"))
                {
                    previewTexture = MiniMapGenerator.CaptureTopDownPreviewForActiveScene(trackData);
                    loadedPreviewPath = trackData.authoringCapture.previewTextureAssetPath;
                    Repaint();
                }

                if (GUILayout.Button("Seed From Checkpoints"))
                {
                    MiniMapGenerator.SeedTrackFromCheckpoints(trackData);
                    Repaint();
                }

                if (GUILayout.Button("Clear Route"))
                {
                    ClearRoute();
                }
            }
        }

        if (trackData != null)
        {
            int routePointCount = trackData.authoredStrokes != null && trackData.authoredStrokes.Count > 0
                ? CountAuthoredStrokePoints(trackData.authoredStrokes)
                : trackData.authoredNormalizedTrackPoints.Count;
            string routeSummary = isDrawingStroke
                ? $"Drawing Samples: {activeStrokePoints.Count}"
                : $"Authored Strokes: {trackData.authoredStrokes.Count} | Route Points: {routePointCount} | Open";
            EditorGUILayout.LabelField(routeSummary);
        }
    }

    private void DrawPrimaryAction()
    {
        using (new EditorGUI.DisabledScope(settings == null || trackData == null || !trackData.HasAuthoredTrack))
        {
            if (GUILayout.Button("Bake + Install In Active Scene"))
            {
                MiniMapTrackData bakedTrack = MiniMapGenerator.GenerateTrackDataForActiveScene(settings, trackData);
                if (bakedTrack != null)
                {
                    MiniMapGenerator.InstallInActiveScene(settings, bakedTrack);
                }
            }
        }
    }

    private void DrawAuthoringPreview(Rect rect)
    {
        ReloadPreviewTexture();
        EditorGUI.DrawRect(rect, new Color(0.08f, 0.10f, 0.14f, 1f));

        if (trackData == null || !trackData.HasAuthoringPreview || previewTexture == null)
        {
            GUI.Label(rect, "Capture a top-down map to start authoring the route.", EditorStyles.centeredGreyMiniLabel);
            isDrawingStroke = false;
            activeStrokePoints.Clear();
            return;
        }

        GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.10f));

        HandlePreviewInput(rect);
        DrawCheckpointOverlay(rect);
        DrawRouteOverlay(rect);
        DrawStrokeOverlay(rect);
    }

    private void DrawCheckpointOverlay(Rect rect)
    {
        if (trackData == null || !trackData.HasAuthoringPreview)
            return;

        List<MiniMapCheckpointData> checkpoints = MiniMapTrackSource.CollectCheckpoints(MiniMapTrackSource.FindCheckpointsRoot());
        Transform startLine = MiniMapTrackSource.FindStartLineTransform();

        Handles.BeginGUI();
        for (int i = 0; i < checkpoints.Count; i++)
        {
            Vector2 point = ToGuiPoint(trackData.authoringCapture.WorldToNormalized(checkpoints[i].worldPosition), rect);
            Handles.color = new Color(1f, 0.82f, 0.28f, 0.95f);
            Handles.DrawSolidDisc(point, Vector3.forward, 4f);
        }

        if (startLine != null)
        {
            Vector2 center = ToGuiPoint(trackData.authoringCapture.WorldToNormalized(startLine.position), rect);
            Vector2 tangent = new Vector2(startLine.forward.x, startLine.forward.z);
            if (tangent.sqrMagnitude <= 0.0001f)
            {
                tangent = Vector2.up;
            }

            tangent.Normalize();
            Vector2 guiTangent = new Vector2(tangent.x, -tangent.y);
            Vector2 perpendicular = new Vector2(-guiTangent.y, guiTangent.x);
            Vector2 lineStart = center - perpendicular * 12f;
            Vector2 lineEnd = center + perpendicular * 12f;
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(4f, lineStart, lineEnd);
        }
        Handles.EndGUI();
    }

    private void DrawRouteOverlay(Rect rect)
    {
        if (trackData == null)
            return;

        Handles.BeginGUI();
        bool drewAnyStroke = false;

        if (trackData.authoredStrokes != null && trackData.authoredStrokes.Count > 0)
        {
            for (int i = 0; i < trackData.authoredStrokes.Count; i++)
            {
                MiniMapAuthoredStroke stroke = trackData.authoredStrokes[i];
                if (stroke == null || stroke.normalizedPoints == null || stroke.normalizedPoints.Count < 2)
                    continue;

                DrawPreviewStroke(stroke.normalizedPoints, rect);
                drewAnyStroke = true;
            }
        }

        if (!drewAnyStroke && trackData.authoredNormalizedTrackPoints.Count >= 2)
        {
            DrawPreviewStroke(trackData.authoredNormalizedTrackPoints, rect);
        }

        Handles.EndGUI();
    }

    private void DrawStrokeOverlay(Rect rect)
    {
        if (!isDrawingStroke || activeStrokePoints.Count < 2)
            return;

        Handles.BeginGUI();

        Vector3[] stroke = BuildGuiPolyline(activeStrokePoints, rect);

        Handles.color = new Color(0f, 0f, 0f, 0.7f);
        Handles.DrawAAPolyLine(6f, stroke);
        Handles.color = new Color(1f, 0.35f, 0.25f, 1f);
        Handles.DrawAAPolyLine(3f, stroke);

        Handles.EndGUI();
    }

    private void HandlePreviewInput(Rect rect)
    {
        if (trackData == null)
            return;

        UnityEngine.Event current = UnityEngine.Event.current;
        Vector2 mousePosition = current.mousePosition;

        switch (current.type)
        {
            case EventType.MouseDown:
                if (current.button == 0 && rect.Contains(mousePosition))
                {
                    BeginStroke(mousePosition, rect);
                    current.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isDrawingStroke)
                {
                    AppendStrokePoint(mousePosition, rect);
                    current.Use();
                }
                break;

            case EventType.MouseUp:
                if (isDrawingStroke && current.button == 0)
                {
                    AppendStrokePoint(mousePosition, rect, force: true);
                    FinalizeStroke();
                    current.Use();
                }
                break;
        }
    }

    private void BeginStroke(Vector2 mousePosition, Rect rect)
    {
        isDrawingStroke = true;
        activeStrokePoints.Clear();
        AppendStrokePoint(mousePosition, rect, force: true);
        Repaint();
    }

    private void AppendStrokePoint(Vector2 mousePosition, Rect rect, bool force = false)
    {
        Vector2 normalizedPoint = NormalizeMousePosition(mousePosition, rect);
        if (!force && activeStrokePoints.Count > 0)
        {
            Vector2 lastGuiPoint = ToGuiPoint(activeStrokePoints[activeStrokePoints.Count - 1], rect);
            if (Vector2.Distance(lastGuiPoint, mousePosition) < ScreenStrokeSampleDistance)
                return;
        }

        activeStrokePoints.Add(ClampNormalized(normalizedPoint));
        Repaint();
    }

    private void FinalizeStroke()
    {
        GeneratedStroke generatedStroke = BuildGeneratedStroke(activeStrokePoints);
        isDrawingStroke = false;
        activeStrokePoints.Clear();

        if (trackData == null || !generatedStroke.IsValid)
        {
            Repaint();
            return;
        }

        EnsureStrokeCollectionSyncedFromGeneratedRoute();

        Undo.RecordObject(trackData, "Draw MiniMap Track Route");
        trackData.authoredStrokes.Add(new MiniMapAuthoredStroke
        {
            normalizedPoints = new List<Vector2>(generatedStroke.Points)
        });

        RebuildFlattenedAuthoringCache();
        EditorUtility.SetDirty(trackData);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    private void ClearRoute()
    {
        if (trackData == null)
            return;

        if (trackData.authoredNormalizedTrackPoints.Count == 0 &&
            trackData.authoredStrokes.Count == 0 &&
            activeStrokePoints.Count == 0 &&
            !isDrawingStroke)
            return;

        Undo.RecordObject(trackData, "Clear MiniMap Track Route");
        trackData.authoredStrokes.Clear();
        trackData.authoredNormalizedTrackPoints.Clear();
        isDrawingStroke = false;
        activeStrokePoints.Clear();
        EditorUtility.SetDirty(trackData);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    private void EnsureStrokeCollectionSyncedFromGeneratedRoute()
    {
        if (trackData == null || trackData.authoredStrokes.Count > 0 || trackData.authoredNormalizedTrackPoints.Count < 2)
            return;

        trackData.authoredStrokes.Add(new MiniMapAuthoredStroke
        {
            normalizedPoints = new List<Vector2>(trackData.authoredNormalizedTrackPoints)
        });
    }

    private void RebuildFlattenedAuthoringCache()
    {
        if (trackData == null)
            return;

        trackData.authoredNormalizedTrackPoints.Clear();
        if (trackData.authoredStrokes == null)
            return;

        for (int i = 0; i < trackData.authoredStrokes.Count; i++)
        {
            MiniMapAuthoredStroke stroke = trackData.authoredStrokes[i];
            if (stroke == null || stroke.normalizedPoints == null || stroke.normalizedPoints.Count < 2)
                continue;

            trackData.authoredNormalizedTrackPoints.AddRange(stroke.normalizedPoints);
        }
    }

    private static GeneratedStroke BuildGeneratedStroke(List<Vector2> rawStroke)
    {
        List<Vector2> filteredStroke = FilterStroke(rawStroke, NormalizedStrokeMergeDistance);
        return new GeneratedStroke(GenerateOpenRouteFromPolyline(filteredStroke));
    }

    private static int CountAuthoredStrokePoints(List<MiniMapAuthoredStroke> authoredStrokes)
    {
        if (authoredStrokes == null)
            return 0;

        int count = 0;
        for (int i = 0; i < authoredStrokes.Count; i++)
        {
            MiniMapAuthoredStroke stroke = authoredStrokes[i];
            if (stroke == null || stroke.normalizedPoints == null)
                continue;

            count += stroke.normalizedPoints.Count;
        }

        return count;
    }

    private static void DrawPreviewStroke(List<Vector2> points, Rect rect)
    {
        if (points == null || points.Count < 2)
            return;

        Vector3[] route = BuildGuiPolyline(points, rect);
        Handles.color = new Color(0.03f, 0.04f, 0.05f, 0.95f);
        Handles.DrawAAPolyLine(8f, route);
        Handles.color = Color.white;
        Handles.DrawAAPolyLine(4f, route);
    }

    private static List<Vector2> GenerateOpenRouteFromPolyline(List<Vector2> basePoints)
    {
        List<Vector2> workingPoints = FilterStroke(basePoints, NormalizedStrokeMergeDistance);
        if (workingPoints.Count < 2)
            return new List<Vector2>();

        List<Vector2> smoothedRoute = workingPoints;
        for (int i = 0; i < ChaikinIterations; i++)
        {
            smoothedRoute = ApplyChaikinOpen(smoothedRoute);
        }

        smoothedRoute = FilterStroke(smoothedRoute, NormalizedStrokeMergeDistance * 0.5f);
        if (smoothedRoute.Count < 2)
            return new List<Vector2>();

        float routeLength = CalculateOpenPathLength(smoothedRoute);
        if (routeLength <= 0.0001f)
            return new List<Vector2>(smoothedRoute);

        int targetPointCount = Mathf.Clamp(
            Mathf.RoundToInt(routeLength / ResampleSpacing),
            MinimumGeneratedPointCount,
            MaximumGeneratedPointCount);

        return FilterStroke(ResampleOpenPath(smoothedRoute, targetPointCount), NormalizedStrokeMergeDistance * 0.5f);
    }

    private void ReloadAssets()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        EnsureSceneAssetsLoaded(activeScene);
        ReloadPreviewTexture();
    }

    private void EnsureSceneAssetsLoaded(Scene activeScene)
    {
        if (settings == null && activeScene.IsValid())
        {
            settings = MiniMapGenerator.GetOrCreateSettingsAsset();
        }

        if (trackData == null && activeScene.IsValid())
        {
            trackData = MiniMapGenerator.GetOrCreateTrackDataAsset(activeScene.name);
        }
    }

    private void EnsureTrackDataCollections()
    {
        if (trackData == null)
            return;

        trackData.authoredStrokes ??= new List<MiniMapAuthoredStroke>();
        trackData.authoredNormalizedTrackPoints ??= new List<Vector2>();
    }

    private void ReloadPreviewTexture()
    {
        string desiredPath = trackData != null ? trackData.authoringCapture.previewTextureAssetPath : null;
        if (loadedPreviewPath == desiredPath && previewTexture != null)
            return;

        loadedPreviewPath = desiredPath;
        previewTexture = MiniMapGenerator.LoadPreviewTexture(trackData);
    }

    private float GetPreviewAspect()
    {
        ReloadPreviewTexture();
        if (previewTexture == null || previewTexture.height == 0)
            return 1f;

        return previewTexture.width / (float)previewTexture.height;
    }

    private static Vector3[] BuildGuiPolyline(List<Vector2> normalizedPoints, Rect rect)
    {
        Vector3[] polyline = new Vector3[normalizedPoints.Count];
        for (int i = 0; i < normalizedPoints.Count; i++)
        {
            polyline[i] = ToGuiPoint(normalizedPoints[i], rect);
        }
        return polyline;
    }

    private static Vector2 NormalizeMousePosition(Vector2 mousePosition, Rect rect)
    {
        return ClampNormalized(new Vector2(
            Mathf.InverseLerp(rect.xMin, rect.xMax, mousePosition.x),
            Mathf.InverseLerp(rect.yMax, rect.yMin, mousePosition.y)));
    }

    private static Vector2 ToGuiPoint(Vector2 normalizedPoint, Rect rect)
    {
        return new Vector2(
            Mathf.Lerp(rect.xMin, rect.xMax, normalizedPoint.x),
            Mathf.Lerp(rect.yMax, rect.yMin, normalizedPoint.y));
    }

    private static Vector2 ClampNormalized(Vector2 normalizedPoint)
    {
        return new Vector2(
            Mathf.Clamp01(normalizedPoint.x),
            Mathf.Clamp01(normalizedPoint.y));
    }

    private static List<Vector2> FilterStroke(List<Vector2> points, float minimumDistance)
    {
        List<Vector2> filtered = new List<Vector2>();
        if (points == null)
            return filtered;

        float minDistanceSq = minimumDistance * minimumDistance;
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 clampedPoint = ClampNormalized(points[i]);
            if (filtered.Count == 0 || (clampedPoint - filtered[filtered.Count - 1]).sqrMagnitude >= minDistanceSq)
            {
                filtered.Add(clampedPoint);
            }
        }

        return filtered;
    }

    private static List<Vector2> ApplyChaikinOpen(List<Vector2> points)
    {
        if (points == null || points.Count < 2)
            return points != null ? new List<Vector2>(points) : new List<Vector2>();

        if (points.Count == 2)
        {
            return new List<Vector2>
            {
                points[0],
                Vector2.Lerp(points[0], points[1], 0.5f),
                points[1]
            };
        }

        List<Vector2> smoothed = new List<Vector2>(points.Count * 2);
        smoothed.Add(points[0]);
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[i + 1];
            smoothed.Add(Vector2.Lerp(current, next, 0.25f));
            smoothed.Add(Vector2.Lerp(current, next, 0.75f));
        }

        smoothed.Add(points[points.Count - 1]);
        return smoothed;
    }

    private static float CalculateOpenPathLength(List<Vector2> points)
    {
        if (points == null || points.Count < 2)
            return 0f;

        float pathLength = 0f;
        for (int i = 0; i < points.Count - 1; i++)
        {
            pathLength += Vector2.Distance(points[i], points[i + 1]);
        }

        return pathLength;
    }

    private static List<Vector2> ResampleOpenPath(List<Vector2> points, int targetPointCount)
    {
        List<Vector2> resampled = new List<Vector2>();
        if (points == null || points.Count < 2)
            return resampled;

        if (targetPointCount <= 2)
        {
            resampled.Add(points[0]);
            resampled.Add(points[points.Count - 1]);
            return resampled;
        }

        float[] cumulativeDistances = new float[points.Count];
        for (int i = 1; i < points.Count; i++)
        {
            cumulativeDistances[i] = cumulativeDistances[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        }

        float totalLength = cumulativeDistances[cumulativeDistances.Length - 1];
        if (totalLength <= 0.0001f)
            return new List<Vector2>(points);

        for (int sampleIndex = 0; sampleIndex < targetPointCount; sampleIndex++)
        {
            float t = sampleIndex / (float)(targetPointCount - 1);
            float targetDistance = totalLength * t;

            int segmentIndex = 1;
            while (segmentIndex < cumulativeDistances.Length && cumulativeDistances[segmentIndex] < targetDistance)
            {
                segmentIndex++;
            }

            if (segmentIndex >= points.Count)
            {
                resampled.Add(points[points.Count - 1]);
                continue;
            }

            Vector2 start = points[segmentIndex - 1];
            Vector2 end = points[segmentIndex];
            float segmentStartDistance = cumulativeDistances[segmentIndex - 1];
            float segmentLength = cumulativeDistances[segmentIndex] - segmentStartDistance;
            float lerpT = segmentLength <= 0.0001f
                ? 0f
                : Mathf.Clamp01((targetDistance - segmentStartDistance) / segmentLength);
            resampled.Add(Vector2.Lerp(start, end, lerpT));
        }

        return resampled;
    }
}
