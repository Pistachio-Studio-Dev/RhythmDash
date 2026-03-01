using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

public static class MiniMapTrackSource
{
    public static MiniMapTrackSourceResult ResolveBaked(MiniMapTrackData bakedTrackData)
    {
        return MiniMapTrackSourceResult.FromAsset(bakedTrackData);
    }

#if UNITY_EDITOR
    public static MiniMapTrackSourceResult BuildFromAuthoring(
        MiniMapSettings settings,
        MiniMapTrackData trackData,
        Transform checkpointsRoot = null,
        Transform startLineTransform = null)
    {
        if (settings == null || trackData == null || !trackData.HasAuthoredTrack)
            return null;

        checkpointsRoot ??= FindCheckpointsRoot();
        startLineTransform ??= FindStartLineTransform();

        List<MiniMapCheckpointData> checkpoints = CollectCheckpoints(checkpointsRoot);
        List<List<Vector3>> routeSegments = CollectAuthoredRouteSegments(trackData);
        List<Vector3> routePoints = FlattenRouteSegments(routeSegments);
        if (routePoints.Count < 2)
            return null;

        MiniMapProjectionSnapshot projection = MiniMapProjection.BuildSnapshot(
            routePoints,
            settings.trackPaddingRatio,
            settings.useManualRotationOverride,
            settings.manualRotationOffsetDegrees);

        if (!projection.IsValid)
            return null;

        MiniMapTrackSourceResult result = new MiniMapTrackSourceResult
        {
            Projection = projection
        };

        for (int i = 0; i < routePoints.Count; i++)
        {
            result.WorldTrackPoints.Add(routePoints[i]);
            result.NormalizedTrackPoints.Add(projection.ProjectWorldPoint(routePoints[i]));
        }

        for (int i = 0; i < routeSegments.Count; i++)
        {
            if (routeSegments[i] != null && routeSegments[i].Count > 1)
            {
                result.SegmentPointCounts.Add(routeSegments[i].Count);
            }
        }

        for (int i = 0; i < checkpoints.Count; i++)
        {
            MiniMapCheckpointData checkpoint = checkpoints[i];
            checkpoint.normalizedPosition = projection.ProjectWorldPoint(checkpoint.worldPosition);
            result.Checkpoints.Add(checkpoint);
        }

        result.StartGate = BuildStartGate(startLineTransform, routeSegments, projection, checkpoints);
        return result;
    }

    public static Transform FindCheckpointsRoot()
    {
        GameObject checkpointsObject = GameObject.Find(GlobalConfig.Instance.CheckpointName);
        return checkpointsObject != null ? checkpointsObject.transform : null;
    }

    public static Transform FindStartLineTransform()
    {
        GameObject startLineObject = GameObject.Find(GlobalConfig.Instance.GateObjectName);
        return startLineObject != null ? startLineObject.transform : null;
    }

    public static List<MiniMapCheckpointData> CollectCheckpoints(Transform checkpointsRoot)
    {
        List<MiniMapCheckpointData> checkpoints = new List<MiniMapCheckpointData>();
        if (checkpointsRoot == null)
            return checkpoints;

        for (int i = 0; i < checkpointsRoot.childCount; i++)
        {
            Transform checkpoint = checkpointsRoot.GetChild(i);
            checkpoints.Add(new MiniMapCheckpointData
            {
                index = i,
                worldPosition = checkpoint.position,
                normalizedPosition = Vector2.zero
            });
        }

        return checkpoints;
    }

    public static List<Vector2> BuildCheckpointSeed(MiniMapTrackData trackData, Transform checkpointsRoot = null)
    {
        List<Vector2> seed = new List<Vector2>();
        if (trackData == null || !trackData.HasAuthoringPreview)
            return seed;

        checkpointsRoot ??= FindCheckpointsRoot();
        List<MiniMapCheckpointData> checkpoints = CollectCheckpoints(checkpointsRoot);
        for (int i = 0; i < checkpoints.Count; i++)
        {
            seed.Add(trackData.authoringCapture.WorldToNormalized(checkpoints[i].worldPosition));
        }

        return seed;
    }

    public static bool TryCalculateAuthoringCapture(out MiniMapAuthoringCaptureData captureData, float paddingWorldUnits = 12f)
    {
        captureData = default;
        if (!TryCalculateSceneBounds(out Bounds bounds))
            return false;

        Vector2 center = new Vector2(bounds.center.x, bounds.center.z);
        float halfSpan = Mathf.Max(bounds.extents.x, bounds.extents.z) + Mathf.Max(6f, paddingWorldUnits);

        captureData.worldMin = center - Vector2.one * halfSpan;
        captureData.worldMax = center + Vector2.one * halfSpan;
        captureData.cameraHeight = bounds.max.y + Mathf.Max(40f, bounds.size.y + 20f);
        captureData.previewResolution = 2048;
        return true;
    }

    private static List<List<Vector3>> CollectAuthoredRouteSegments(MiniMapTrackData trackData)
    {
        List<List<Vector3>> routeSegments = new List<List<Vector3>>();
        if (trackData == null)
            return routeSegments;

        if (trackData.authoredStrokes != null && trackData.authoredStrokes.Count > 0)
        {
            for (int i = 0; i < trackData.authoredStrokes.Count; i++)
            {
                MiniMapAuthoredStroke stroke = trackData.authoredStrokes[i];
                if (stroke == null || stroke.normalizedPoints == null || stroke.normalizedPoints.Count < 2)
                    continue;

                List<Vector3> worldPoints = new List<Vector3>(stroke.normalizedPoints.Count);
                for (int pointIndex = 0; pointIndex < stroke.normalizedPoints.Count; pointIndex++)
                {
                    worldPoints.Add(trackData.authoringCapture.NormalizedToWorldPoint(stroke.normalizedPoints[pointIndex]));
                }

                routeSegments.Add(worldPoints);
            }
        }

        if (routeSegments.Count == 0 && trackData.authoredNormalizedTrackPoints != null && trackData.authoredNormalizedTrackPoints.Count > 1)
        {
            List<Vector3> fallbackRoute = new List<Vector3>(trackData.authoredNormalizedTrackPoints.Count);
            for (int i = 0; i < trackData.authoredNormalizedTrackPoints.Count; i++)
            {
                fallbackRoute.Add(trackData.authoringCapture.NormalizedToWorldPoint(trackData.authoredNormalizedTrackPoints[i]));
            }

            routeSegments.Add(fallbackRoute);
        }

        return routeSegments;
    }

    private static List<Vector3> FlattenRouteSegments(List<List<Vector3>> routeSegments)
    {
        List<Vector3> flattened = new List<Vector3>();
        if (routeSegments == null)
            return flattened;

        for (int i = 0; i < routeSegments.Count; i++)
        {
            if (routeSegments[i] == null || routeSegments[i].Count < 2)
                continue;

            flattened.AddRange(routeSegments[i]);
        }

        return flattened;
    }

    private static MiniMapStartGateData BuildStartGate(
        Transform startLineTransform,
        List<List<Vector3>> routeSegments,
        MiniMapProjectionSnapshot projection,
        List<MiniMapCheckpointData> checkpoints)
    {
        MiniMapStartGateData startGate = default;
        List<Vector3> flattenedRoute = FlattenRouteSegments(routeSegments);
        if (flattenedRoute.Count == 0)
            return startGate;

        Vector3 worldPosition = startLineTransform != null
            ? startLineTransform.position
            : checkpoints.Count > 0
                ? checkpoints[0].worldPosition
                : flattenedRoute[0];

        Vector3 worldTangent = EstimateRouteTangent(routeSegments, worldPosition);
        if (worldTangent.sqrMagnitude <= 0.0001f && startLineTransform != null)
        {
            worldTangent = startLineTransform.forward;
        }

        if (worldTangent.sqrMagnitude <= 0.0001f && flattenedRoute.Count > 1)
        {
            worldTangent = flattenedRoute[1] - flattenedRoute[0];
        }

        startGate.isValid = true;
        startGate.worldPosition = worldPosition;
        startGate.worldTangent = worldTangent.normalized;
        startGate.normalizedPosition = projection.ProjectWorldPoint(worldPosition);
        startGate.normalizedTangent = projection.ProjectWorldDirection(worldTangent);
        return startGate;
    }

    private static Vector3 EstimateRouteTangent(List<List<Vector3>> routeSegments, Vector3 worldPosition)
    {
        if (routeSegments == null || routeSegments.Count == 0)
            return Vector3.forward;

        float bestDistance = float.PositiveInfinity;
        List<Vector3> bestSegment = null;
        int bestIndex = 0;

        for (int segmentIndex = 0; segmentIndex < routeSegments.Count; segmentIndex++)
        {
            List<Vector3> segment = routeSegments[segmentIndex];
            if (segment == null || segment.Count < 2)
                continue;

            for (int pointIndex = 0; pointIndex < segment.Count; pointIndex++)
            {
                float distance = (segment[pointIndex] - worldPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestSegment = segment;
                    bestIndex = pointIndex;
                }
            }
        }

        if (bestSegment == null || bestSegment.Count < 2)
            return Vector3.forward;

        if (bestIndex <= 0)
            return (bestSegment[1] - bestSegment[0]).normalized;

        if (bestIndex >= bestSegment.Count - 1)
            return (bestSegment[bestSegment.Count - 1] - bestSegment[bestSegment.Count - 2]).normalized;

        return (bestSegment[bestIndex + 1] - bestSegment[bestIndex - 1]).normalized;
    }

    private static bool TryCalculateSceneBounds(out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        Scene activeScene = SceneManager.GetActiveScene();

        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer.gameObject.scene != activeScene)
                continue;

            if (renderer.GetComponentInParent<Canvas>() != null)
                continue;

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || collider.gameObject.scene != activeScene)
                continue;

            if (!hasBounds)
            {
                bounds = collider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        Transform checkpointsRoot = FindCheckpointsRoot();
        List<MiniMapCheckpointData> checkpoints = CollectCheckpoints(checkpointsRoot);
        for (int i = 0; i < checkpoints.Count; i++)
        {
            EncapsulatePoint(checkpoints[i].worldPosition, ref hasBounds, ref bounds);
        }

        Transform startLine = FindStartLineTransform();
        if (startLine != null)
        {
            EncapsulatePoint(startLine.position, ref hasBounds, ref bounds);
        }

        return hasBounds;
    }

    private static void EncapsulatePoint(Vector3 point, ref bool hasBounds, ref Bounds bounds)
    {
        if (!hasBounds)
        {
            bounds = new Bounds(point, Vector3.one);
            hasBounds = true;
            return;
        }

        bounds.Encapsulate(point);
    }
#endif
}
