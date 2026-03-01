using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MiniMapMeshBuilder
{
    public static void AddQuad(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 color)
    {
        int startIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(a, color, Vector2.zero);
        vertexHelper.AddVert(b, color, Vector2.right);
        vertexHelper.AddVert(c, color, Vector2.one);
        vertexHelper.AddVert(d, color, Vector2.up);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    public static void AddQuad(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 colorA, Color32 colorB, Color32 colorC, Color32 colorD)
    {
        int startIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(a, colorA, Vector2.zero);
        vertexHelper.AddVert(b, colorB, Vector2.right);
        vertexHelper.AddVert(c, colorC, Vector2.one);
        vertexHelper.AddVert(d, colorD, Vector2.up);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    public static void AddTriangle(VertexHelper vertexHelper, Vector2 a, Vector2 b, Vector2 c, Color32 color)
    {
        int startIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(a, color, Vector2.zero);
        vertexHelper.AddVert(b, color, Vector2.right);
        vertexHelper.AddVert(c, color, Vector2.up);
        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
    }

    public static void AddFilledConvexPolygon(VertexHelper vertexHelper, IList<Vector2> points, Color32 color)
    {
        if (points == null || points.Count < 3)
            return;

        int startIndex = vertexHelper.currentVertCount;
        for (int i = 0; i < points.Count; i++)
        {
            vertexHelper.AddVert(points[i], color, Vector2.zero);
        }

        for (int i = 1; i < points.Count - 1; i++)
        {
            vertexHelper.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
        }
    }

    public static void AddCircle(VertexHelper vertexHelper, Vector2 center, float radius, Color32 color, int segments = 16)
    {
        if (radius <= 0f)
            return;

        int startIndex = vertexHelper.currentVertCount;
        vertexHelper.AddVert(center, color, Vector2.one * 0.5f);

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.PI * 2f * (i / (float)segments);
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            vertexHelper.AddVert(point, color, Vector2.zero);
        }

        for (int i = 1; i <= segments; i++)
        {
            vertexHelper.AddTriangle(startIndex, startIndex + i, startIndex + i + 1);
        }
    }

    public static void AddLineStrip(VertexHelper vertexHelper, IList<Vector2> points, float width, Color32 color, bool closed)
    {
        if (points == null || points.Count < 2 || width <= 0f)
            return;

        int segmentCount = closed ? points.Count : points.Count - 1;
        float radius = width * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[(i + 1) % points.Count];
            AddLineSegment(vertexHelper, start, end, width, color);
        }

        for (int i = 0; i < points.Count; i++)
        {
            AddCircle(vertexHelper, points[i], radius, color, 12);
        }
    }

    public static void AddLineSegmentGradient(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color32 startColor, Color32 endColor)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.000001f)
            return;

        direction.Normalize();
        Vector2 normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);

        AddQuad(
            vertexHelper,
            start - normal,
            start + normal,
            end + normal,
            end - normal,
            startColor,
            startColor,
            endColor,
            endColor);
    }

    public static void AddRoundedRect(VertexHelper vertexHelper, Rect rect, float radius, Color32 color, int cornerSegments = 6)
    {
        List<Vector2> points = BuildRoundedRectPoints(rect, radius, cornerSegments);
        AddFilledConvexPolygon(vertexHelper, points, color);
    }

    public static List<Vector2> BuildRoundedRectPoints(Rect rect, float radius, int cornerSegments = 6)
    {
        List<Vector2> points = new List<Vector2>(cornerSegments * 4 + 4);
        radius = Mathf.Clamp(radius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);

        AppendArc(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, cornerSegments);
        AppendArc(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, cornerSegments);
        AppendArc(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, cornerSegments);
        AppendArc(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, cornerSegments);
        return points;
    }

    private static void AddLineSegment(VertexHelper vertexHelper, Vector2 start, Vector2 end, float width, Color32 color)
    {
        Vector2 direction = end - start;
        if (direction.sqrMagnitude <= 0.000001f)
            return;

        direction.Normalize();
        Vector2 normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);

        AddQuad(
            vertexHelper,
            start - normal,
            start + normal,
            end + normal,
            end - normal,
            color);
    }

    private static void AppendArc(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle, int segments)
    {
        if (radius <= 0f)
        {
            points.Add(center);
            return;
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
            points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
    }
}
