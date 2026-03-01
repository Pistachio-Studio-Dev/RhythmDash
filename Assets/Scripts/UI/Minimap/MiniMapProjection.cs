using System.Collections.Generic;
using UnityEngine;

public static class MiniMapProjection
{
    public static MiniMapProjectionSnapshot BuildSnapshot(
        IReadOnlyList<Vector3> worldPoints,
        float paddingRatio,
        bool useManualRotationOverride,
        float manualRotationDegrees)
    {
        MiniMapProjectionSnapshot snapshot = default;

        if (worldPoints == null || worldPoints.Count < 2)
            return snapshot;

        Vector2 average = Vector2.zero;
        List<Vector2> points2D = new List<Vector2>(worldPoints.Count);

        for (int i = 0; i < worldPoints.Count; i++)
        {
            Vector2 point = new Vector2(worldPoints[i].x, worldPoints[i].z);
            points2D.Add(point);
            average += point;
        }

        average /= worldPoints.Count;

        float angleRadians = useManualRotationOverride
            ? manualRotationDegrees * Mathf.Deg2Rad
            : ComputePrincipalAngle(points2D, average);

        Vector2 axisX = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)).normalized;
        Vector2 axisY = new Vector2(-axisX.y, axisX.x);

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < points2D.Count; i++)
        {
            Vector2 delta = points2D[i] - average;
            float projectedX = Vector2.Dot(delta, axisX);
            float projectedY = Vector2.Dot(delta, axisY);

            minX = Mathf.Min(minX, projectedX);
            maxX = Mathf.Max(maxX, projectedX);
            minY = Mathf.Min(minY, projectedY);
            maxY = Mathf.Max(maxY, projectedY);
        }

        Vector2 recenteredOffset = axisX * ((minX + maxX) * 0.5f) + axisY * ((minY + maxY) * 0.5f);
        Vector2 extents = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
        float fallbackExtent = Mathf.Max(extents.x, extents.y, 0.5f);

        extents.x = Mathf.Max(extents.x, fallbackExtent * 0.35f);
        extents.y = Mathf.Max(extents.y, fallbackExtent * 0.35f);
        extents *= 1f + Mathf.Max(0f, paddingRatio);

        snapshot.worldCenter = average + recenteredOffset;
        snapshot.axisX = axisX;
        snapshot.axisY = axisY;
        snapshot.extents = extents;
        return snapshot;
    }

    private static float ComputePrincipalAngle(IReadOnlyList<Vector2> points, Vector2 center)
    {
        float covarianceXX = 0f;
        float covarianceYY = 0f;
        float covarianceXY = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 delta = points[i] - center;
            covarianceXX += delta.x * delta.x;
            covarianceYY += delta.y * delta.y;
            covarianceXY += delta.x * delta.y;
        }

        covarianceXX /= points.Count;
        covarianceYY /= points.Count;
        covarianceXY /= points.Count;

        return 0.5f * Mathf.Atan2(2f * covarianceXY, covarianceXX - covarianceYY);
    }

    public static bool TryBuildRadarBasis(
        Vector3 centerWorldPosition,
        Vector3 forwardWorldDirection,
        out Vector2 origin,
        out Vector2 right,
        out Vector2 forward)
    {
        origin = new Vector2(centerWorldPosition.x, centerWorldPosition.z);
        forward = new Vector2(forwardWorldDirection.x, forwardWorldDirection.z);

        if (forward.sqrMagnitude <= 0.000001f)
        {
            forward = Vector2.up;
        }
        else
        {
            forward.Normalize();
        }

        right = new Vector2(forward.y, -forward.x);
        return true;
    }

    public static Vector2 ProjectRadarPoint(
        Vector3 centerWorldPosition,
        Vector3 forwardWorldDirection,
        Vector3 targetWorldPosition,
        float worldRadius)
    {
        TryBuildRadarBasis(centerWorldPosition, forwardWorldDirection, out Vector2 origin, out Vector2 right, out Vector2 forward);
        return ProjectRadarPoint(origin, right, forward, targetWorldPosition, worldRadius);
    }

    public static Vector2 ProjectRadarPoint(
        Vector2 origin,
        Vector2 right,
        Vector2 forward,
        Vector3 targetWorldPosition,
        float worldRadius)
    {
        if (worldRadius <= 0.0001f)
            return Vector2.zero;

        Vector2 delta = new Vector2(targetWorldPosition.x, targetWorldPosition.z) - origin;
        return new Vector2(
            Vector2.Dot(delta, right) / worldRadius,
            Vector2.Dot(delta, forward) / worldRadius);
    }

    public static Vector2 ProjectRadarDirection(
        Vector3 centerWorldForward,
        Vector3 worldDirection)
    {
        TryBuildRadarBasis(Vector3.zero, centerWorldForward, out _, out Vector2 right, out Vector2 forward);
        return ProjectRadarDirection(right, forward, worldDirection);
    }

    public static Vector2 ProjectRadarDirection(
        Vector2 right,
        Vector2 forward,
        Vector3 worldDirection)
    {
        Vector2 direction = new Vector2(worldDirection.x, worldDirection.z);
        if (direction.sqrMagnitude <= 0.000001f)
            return Vector2.up;

        direction.Normalize();

        Vector2 projected = new Vector2(
            Vector2.Dot(direction, right),
            Vector2.Dot(direction, forward));

        return projected.sqrMagnitude > 0.000001f ? projected.normalized : Vector2.up;
    }
}
