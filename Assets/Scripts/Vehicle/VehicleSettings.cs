using UnityEngine;

[CreateAssetMenu(fileName = "VehicleSettings", menuName = "Vehicle/VehicleSettings")]
public class VehicleSettings : ScriptableObject
{
    public float vehiclemass = 1.0f;
    [Tooltip("Directly multiplied to the curve result below")]
    public float curveMuliplier = 1.0f;

    [Tooltip("X: Speed (m/s), Y: Force (N)")]
    public AnimationCurve cruiseCurve;
    [Tooltip("X: Speed (m/s), Y: Force (N)")]
    public AnimationCurve sprintCurve;

    [Header("Motion Profiles")]
    public float autoMaxSpeed = 5f;
    public float sprintMaxSpeed = 20f;

    //[Header("Steering")]
    //public float steerTorqueHighSpeed = 5f;
    //public float steerTorqueLowSpeed = 10f;
    //public float steerResistanceTorque = 30f;

    //[Header("Lateral Grip")]
    //public float staticGripForce = 60f;
    //public float maxSlidingGrip = 40f;
    //public float staticSlipFactor = 0.9f;

    //[Header("Suspension Settings")]
    //public float suspensionRaycastDist = 1f;
    //public float suspensionRestDist = 0.2f;
    //public float springStrength = 50000f;
    //public float springDamper = 40000f;

    public float maxSteerAngleLowSpeed = 150f;
    public float maxSteerAngleHighSpeed = 120f;

    public float maxSlidingGripFactor = 0.268f;//tan(15)
    public float gripRecoveryFactor = 0.176f;//tan(10)
    public float slideDeceleration = 1.0f;

    public float burstPushForce = 2.0f;
}
