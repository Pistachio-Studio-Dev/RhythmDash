using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct MiniMapProjectionSnapshot
{
    public Vector2 worldCenter;
    public Vector2 axisX;
    public Vector2 axisY;
    public Vector2 extents;

    public bool IsValid => extents.x > 0.0001f && extents.y > 0.0001f;

    public Vector2 ProjectWorldPoint(Vector3 worldPosition)
    {
        if (!IsValid)
            return Vector2.zero;

        Vector2 delta = new Vector2(worldPosition.x, worldPosition.z) - worldCenter;
        float localX = Vector2.Dot(delta, axisX);
        float localY = Vector2.Dot(delta, axisY);

        return new Vector2(
            Mathf.Clamp01(0.5f + (localX / (extents.x * 2f))),
            Mathf.Clamp01(0.5f + (localY / (extents.y * 2f))));
    }

    public Vector2 ProjectWorldDirection(Vector3 worldDirection)
    {
        Vector2 direction = new Vector2(worldDirection.x, worldDirection.z);
        if (direction.sqrMagnitude <= 0.000001f)
            return Vector2.up;

        direction.Normalize();

        Vector2 projected = new Vector2(
            Vector2.Dot(direction, axisX),
            Vector2.Dot(direction, axisY));

        return projected.sqrMagnitude > 0.000001f ? projected.normalized : Vector2.up;
    }
}

[System.Serializable]
public struct MiniMapCheckpointData
{
    public int index;
    public Vector3 worldPosition;
    public Vector2 normalizedPosition;
}

[System.Serializable]
public struct MiniMapStartGateData
{
    public bool isValid;
    public Vector3 worldPosition;
    public Vector3 worldTangent;
    public Vector2 normalizedPosition;
    public Vector2 normalizedTangent;
}

[System.Serializable]
public struct MiniMapAuthoringCaptureData
{
    public string previewTextureAssetPath;
    public Vector2 worldMin;
    public Vector2 worldMax;
    public float cameraHeight;
    public int previewResolution;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(previewTextureAssetPath) &&
        worldMax.x > worldMin.x + 0.001f &&
        worldMax.y > worldMin.y + 0.001f;

    public Vector2 WorldToNormalized(Vector3 worldPosition)
    {
        if (!IsValid)
            return Vector2.zero;

        return new Vector2(
            Mathf.InverseLerp(worldMin.x, worldMax.x, worldPosition.x),
            Mathf.InverseLerp(worldMin.y, worldMax.y, worldPosition.z));
    }

    public Vector3 NormalizedToWorldPoint(Vector2 normalizedPoint)
    {
        normalizedPoint.x = Mathf.Clamp01(normalizedPoint.x);
        normalizedPoint.y = Mathf.Clamp01(normalizedPoint.y);

        return new Vector3(
            Mathf.Lerp(worldMin.x, worldMax.x, normalizedPoint.x),
            0f,
            Mathf.Lerp(worldMin.y, worldMax.y, normalizedPoint.y));
    }
}

[System.Serializable]
public class MiniMapAuthoredStroke
{
    public List<Vector2> normalizedPoints = new List<Vector2>();
}

[CreateAssetMenu(fileName = "MiniMapTrackData", menuName = "UI/MiniMap Track Data")]
public class MiniMapTrackData : ScriptableObject
{
    public string sceneName;
    public MiniMapProjectionSnapshot projection;
    public List<int> trackSegmentPointCounts = new List<int>();
    public List<Vector3> worldTrackPoints = new List<Vector3>();
    public List<Vector2> normalizedTrackPoints = new List<Vector2>();
    public List<MiniMapCheckpointData> checkpoints = new List<MiniMapCheckpointData>();
    public MiniMapStartGateData startGate;

    [Header("Editor Authoring")]
    public MiniMapAuthoringCaptureData authoringCapture;
    public List<MiniMapAuthoredStroke> authoredStrokes = new List<MiniMapAuthoredStroke>();
    public List<Vector2> authoredNormalizedTrackPoints = new List<Vector2>();

    public bool HasData => projection.IsValid && normalizedTrackPoints != null && normalizedTrackPoints.Count > 1;
    public bool HasAuthoringPreview => authoringCapture.IsValid;
    public bool HasAuthoredTrack =>
        HasAuthoringPreview &&
        ((authoredStrokes != null && HasAnyValidStroke(authoredStrokes)) ||
         (authoredNormalizedTrackPoints != null && authoredNormalizedTrackPoints.Count > 1));

    public void CopyFrom(MiniMapTrackSourceResult source, string sourceSceneName)
    {
        sceneName = sourceSceneName;
        projection = source.Projection;

        trackSegmentPointCounts.Clear();
        trackSegmentPointCounts.AddRange(source.SegmentPointCounts);

        worldTrackPoints.Clear();
        worldTrackPoints.AddRange(source.WorldTrackPoints);

        normalizedTrackPoints.Clear();
        normalizedTrackPoints.AddRange(source.NormalizedTrackPoints);

        checkpoints.Clear();
        checkpoints.AddRange(source.Checkpoints);

        startGate = source.StartGate;
    }

    private static bool HasAnyValidStroke(List<MiniMapAuthoredStroke> strokes)
    {
        for (int i = 0; i < strokes.Count; i++)
        {
            MiniMapAuthoredStroke stroke = strokes[i];
            if (stroke != null && stroke.normalizedPoints != null && stroke.normalizedPoints.Count > 1)
                return true;
        }

        return false;
    }
}

public sealed class MiniMapTrackSourceResult
{
    public MiniMapProjectionSnapshot Projection { get; set; }
    public List<int> SegmentPointCounts { get; } = new List<int>();
    public List<Vector3> WorldTrackPoints { get; } = new List<Vector3>();
    public List<Vector2> NormalizedTrackPoints { get; } = new List<Vector2>();
    public List<MiniMapCheckpointData> Checkpoints { get; } = new List<MiniMapCheckpointData>();
    public MiniMapStartGateData StartGate { get; set; }

    public bool IsValid => Projection.IsValid && NormalizedTrackPoints.Count > 1;

    public Vector2 ProjectWorldPoint(Vector3 worldPosition)
    {
        return Projection.ProjectWorldPoint(worldPosition);
    }

    public Vector2 ProjectWorldDirection(Vector3 worldDirection)
    {
        return Projection.ProjectWorldDirection(worldDirection);
    }

    public static MiniMapTrackSourceResult FromAsset(MiniMapTrackData asset)
    {
        if (asset == null || !asset.HasData)
            return null;

        MiniMapTrackSourceResult result = new MiniMapTrackSourceResult
        {
            Projection = asset.projection,
            StartGate = asset.startGate
        };

        if (asset.trackSegmentPointCounts != null && asset.trackSegmentPointCounts.Count > 0)
        {
            result.SegmentPointCounts.AddRange(asset.trackSegmentPointCounts);
        }
        else if (asset.worldTrackPoints != null && asset.worldTrackPoints.Count > 1)
        {
            result.SegmentPointCounts.Add(asset.worldTrackPoints.Count);
        }

        result.WorldTrackPoints.AddRange(asset.worldTrackPoints);
        result.NormalizedTrackPoints.AddRange(asset.normalizedTrackPoints);
        result.Checkpoints.AddRange(asset.checkpoints);
        return result;
    }
}
