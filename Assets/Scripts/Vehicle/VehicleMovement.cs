using Event;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class VehicleMovement : SessionBasedBehaviour
{
    [SerializeField] VehicleSettings _settings;

    public Transform frontLeft;
    public Transform frontRight;
    public Transform rearLeft;
    public Transform rearRight;

    protected float autoMaxSpeed;
    protected float sprintMaxSpeed;
    private float autoResistanceForce;
    private float sprintResistanceForce;

    protected float maxSteerAngleLowSpeed;
    protected float maxSteerAngleHighSpeed;

    //the max velocity side grip can handle, divided by highest speed;
    protected float maxSlidingGripFactor;//tan(15)
    protected float gripRecoveryFactor;//tan(10)
    protected float slideDeceleration;

    protected float throttleInput;
    protected float steerInput;

    protected Rigidbody rb;
    private int groundCount;
    private Vector3 groundNormal;
    private float currentYaw;
    private bool isSliding = false;
    protected bool inSprint;
    protected Vehicle owner;

    //wheels are just used to check if the car is on the ground
    protected Transform[] allWheels;
    private Vector3 pausedLinearVelocity;
    private Vector3 pausedAngularVelocity;
    private RigidbodyConstraints pausedConstraints;
    private bool pausedIsKinematic;
    private bool rigidbodyFrozen;

    bool hasPendingBeatForce = false;

    public float CurrentSpeed => Mathf.Max(0f, rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f);
    protected virtual float SpeedMultiplier => 1f;
    public float AutoSpeed => autoMaxSpeed * SpeedMultiplier;
    public float MaxSpeed => sprintMaxSpeed * SpeedMultiplier;
    public float Throttle => throttleInput;
    public float Steer => steerInput;
    public bool IsGroundedForces => groundCount > 0;
    public bool InSprint => inSprint;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        LoadSettings();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        allWheels = new Transform[] { frontLeft, frontRight, rearLeft, rearRight };
        currentYaw = transform.eulerAngles.y;
    }

    protected override void OnEnable() {
        base.OnEnable();
        EventManager.StartListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
    }

    protected override void OnDisable() {
        if (rb != null && rigidbodyFrozen)
        {
            RestoreRigidbody();
        }

        base.OnDisable();
        EventManager.StopListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
    }

    protected virtual void Start()
    {

    }

    public void SetBeatExtraBurst()
    {
        hasPendingBeatForce = true;
    }

    protected abstract void UpdateInput();

    private void FixedUpdate()
    {
        if (rb == null || isGameplayPaused || (currentSessionState != SessionState.Racing && currentSessionState != SessionState.Finished))
            return;

        UpdateInput();
        CheckGrounded();
        ApplySteering();
        ApplyForwardForces();
        ApplyLateralGrip();
    }

    protected virtual void OnVehicleFinished(VehicleProgress vp) {
        if (vp.transform.name == transform.name) {
            StartCoroutine(PauseVehicle(1f));
        }
    }

    private IEnumerator PauseVehicle(float duration) {
        yield return GameplayClock.WaitForSeconds(duration);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        if (rb == null)
        {
            return;
        }

        if (paused)
        {
            FreezeRigidbody();
            return;
        }

        RestoreRigidbody();
    }

    private void CheckGrounded()
    {
        groundCount = 0;
        groundNormal = Vector3.zero;
        foreach (Transform wheel in allWheels)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, -transform.up, out hit, 1.0f))
            {
                ++groundCount;
                groundNormal += hit.normal;
            }
        }
        if (groundNormal.sqrMagnitude > 0)
        {
            groundNormal.Normalize();
        }
        else
        {
            groundNormal = Vector3.up;
        }
    }

    private void ApplyForwardForces()
    {
        if (!IsGroundedForces)
            return;

        // *new*: wing boost
        float boostedAutoMaxSpeed = autoMaxSpeed * SpeedMultiplier;
        float boostedSprintMaxSpeed = sprintMaxSpeed * SpeedMultiplier;

        float targetMaxSpeed = Mathf.Lerp(boostedAutoMaxSpeed, boostedSprintMaxSpeed, throttleInput);
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, owner.transform.forward));
        float resistanceForce = forwardSpeed > autoMaxSpeed ? sprintResistanceForce : autoResistanceForce;

        float clampMax = inSprint ? _settings.sprintCurve.keys[^1].time : _settings.cruiseCurve.keys[^1].time;
        float speedRatio = Mathf.Clamp(forwardSpeed / Mathf.Max(targetMaxSpeed, 0.001f), 0.0f, clampMax);

        float engineForce = (inSprint ? _settings.sprintCurve.Evaluate(speedRatio) : _settings.cruiseCurve.Evaluate(speedRatio)) * _settings.curveMuliplier;
        engineForce *= SpeedMultiplier;
        
        float totalLongitudinalForce = engineForce - resistanceForce;

        if (hasPendingBeatForce)
        {
            hasPendingBeatForce = false;
            totalLongitudinalForce += _settings.burstPushForce;
        }

        rb.AddForce(owner.transform.forward * totalLongitudinalForce, ForceMode.Force);
    }

    private void ApplyLateralGrip()
    {
        if (!IsGroundedForces)
            return;

        Vector3 currentVelocity = rb.linearVelocity;

        float fwdSpeed = Vector3.Dot(currentVelocity, transform.forward);
        float latSpeed = Vector3.Dot(currentVelocity, transform.right);
        float upSpeed = currentVelocity.y; // Maintain absolute vertical velocity (gravity/bouncing)

        // *new*: wing boost
        float boostedSprintMaxSpeed = sprintMaxSpeed * SpeedMultiplier;
        float maxGripVelocity = maxSlidingGripFactor * boostedSprintMaxSpeed;
        float gripRecoveryVelocity = gripRecoveryFactor * boostedSprintMaxSpeed;

        if (!isSliding)
        {
            // If we have perfect grip, check if the lateral momentum from a sharp turn 
            // or an external impact exceeds our tire's maximum limit.
            if (Mathf.Abs(latSpeed) > maxGripVelocity)
            {
                isSliding = true;
            }
        }
        else
        {
            // If we are currently sliding, wait until the lateral friction 
            // slows us down enough to grab the track again.
            if (Mathf.Abs(latSpeed) < gripRecoveryVelocity)
            {
                isSliding = false;
            }
        }

        // Apply physics based on the current state
        if (!isSliding)
        {

            latSpeed = 0f;
        }
        else
        {
            // SLIDING: Apply constant deceleration (friction) to the lateral movement
            // We use Math.Sign to ensure we always slow down toward 0, regardless of left/right direction.
            float lateralDrag = slideDeceleration * Time.fixedDeltaTime;

            if (latSpeed > 0f)
                latSpeed = Mathf.Max(0f, latSpeed - lateralDrag);
            else if (latSpeed < 0f)
                latSpeed = Mathf.Min(0f, latSpeed + lateralDrag);
        }

        // Reconstruct the velocity vector
        // Because we use transform.forward here, the forward momentum seamlessly curves 
        // to match the nose of the car as it turns, preventing sliding during normal steering.
        Vector3 newVelocity = (transform.forward * fwdSpeed) + (transform.right * latSpeed);
        newVelocity.y = upSpeed;

        rb.linearVelocity = newVelocity;
    }

    private void ApplySteering()
    {
        // wing boost not applied to steering so controls are less twitchy at high speed
        float baseTargetMaxSpeed = Mathf.Lerp(autoMaxSpeed, sprintMaxSpeed, throttleInput);
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(rb.linearVelocity, owner.transform.forward));
        float speedRatio = Mathf.Clamp01(forwardSpeed / Mathf.Max(baseTargetMaxSpeed, 0.001f));

        // At low speed use the low-speed steer angle.
        // At high speed use the high-speed steer angle.
        float currentSteerAngle = steerInput * Mathf.Lerp(maxSteerAngleLowSpeed, maxSteerAngleHighSpeed, speedRatio) * Time.fixedDeltaTime;
        currentYaw += currentSteerAngle;

        Vector3 flatForward = Quaternion.Euler(0f, currentYaw, 0f) * Vector3.forward;

        Vector3 slopeAlignedForward = Vector3.ProjectOnPlane(flatForward, groundNormal).normalized;

        if (slopeAlignedForward.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(slopeAlignedForward, groundNormal);

            rb.MoveRotation(targetRotation);
        }
    }

    private void LoadSettings()
    {
        if (_settings != null)
        {
            rb.mass = _settings.vehiclemass;
            autoMaxSpeed = _settings.autoMaxSpeed;
            sprintMaxSpeed = _settings.sprintMaxSpeed;
            autoResistanceForce = _settings.cruiseCurve.Evaluate(1f);
            sprintResistanceForce = _settings.sprintCurve.Evaluate(1f);
            maxSteerAngleLowSpeed = _settings.maxSteerAngleLowSpeed;
            maxSteerAngleHighSpeed = _settings.maxSteerAngleHighSpeed;

            //the max velocity side grip can handle, divided by highest speed;
            maxSlidingGripFactor = _settings.maxSlidingGripFactor;//tan(15)
            gripRecoveryFactor = _settings.gripRecoveryFactor;//tan(10)
            slideDeceleration = _settings.slideDeceleration;
        }
        else
        {
            Debug.LogError($"[{name}] VehicleMovement expects a VehicleSettings on this object.", this);
        }
    }

    public void SetOwner(Vehicle inV)
    {
        owner = inV;
    }

    public void FreezeRigidbody()
    {
        if (rigidbodyFrozen)
        {
            return;
        }

        pausedLinearVelocity = rb.linearVelocity;
        pausedAngularVelocity = rb.angularVelocity;
        pausedConstraints = rb.constraints;
        pausedIsKinematic = rb.isKinematic;
        throttleInput = 0f;
        steerInput = 0f;
        inSprint = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        rigidbodyFrozen = true;
    }

    public void RestoreRigidbody()
    {
        if (!rigidbodyFrozen)
        {
            return;
        }

        rb.isKinematic = pausedIsKinematic;
        rb.constraints = pausedConstraints;
        // rb.linearVelocity = pausedLinearVelocity;
        float speed = pausedLinearVelocity.magnitude;
        rb.linearVelocity = transform.forward * speed;
        rb.angularVelocity = pausedAngularVelocity;
        rigidbodyFrozen = false;
    }

    public void SetYaw(float newYaw)
    {
        currentYaw = newYaw;
    }
}
