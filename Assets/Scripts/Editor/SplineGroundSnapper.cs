using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;
using Unity.Mathematics;

public class SplineGroundSnapper : EditorWindow
{
    [MenuItem("Tools/Spline/Snapper")]
    public static void SnapSplineToGround()
    {
        GameObject selection = Selection.activeGameObject;
        if (selection == null || !selection.TryGetComponent<SplineContainer>(out var container))
        {
            Debug.LogError("Select SplineContainer£¡");
            return;
        }

        Undo.RecordObject(container, "Snap Spline to Ground");

        foreach (var spline in container.Splines)
        {
            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot knot = spline[i];
                float3 worldPos = math.transform(selection.transform.localToWorldMatrix, knot.Position);

                Ray ray = new Ray((Vector3)worldPos + Vector3.up * 10f, Vector3.down);
                if (Physics.Raycast(ray, out RaycastHit hit, 50f))
                {
                    float3 localHitPos = math.transform(selection.transform.worldToLocalMatrix, (float3)hit.point);
                    knot.Position = localHitPos;
                    knot.Rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                }

                spline[i] = knot;
            }
        }

        Debug.Log($"Spline '{selection.name}' Snapper Completed");
    }
}