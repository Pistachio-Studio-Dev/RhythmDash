using UnityEngine;
using UnityEngine.Splines;

// Not being used for now.
public class DebugByTeleport : MonoBehaviour
{
    public SplineContainer spline;    
    private Rigidbody rb;
    
    private void Start() {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) Teleport(0.1f);
        if (Input.GetKeyDown(KeyCode.Alpha0)) Teleport(0.2f);
    }

    private void Teleport(float t) {
        Vector3 pos = spline.EvaluatePosition(t);
        Vector3 tan = spline.EvaluateTangent(t);
        Vector3 up = spline.EvaluateUpVector(t);
        Quaternion rot = Quaternion.LookRotation(tan, up);

        if (rb) {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos + up*0.2f;
            rb.rotation = rot;
        } else {
            Debug.Log($"Rigidbody not found on {name}.");
        }
    }
}
