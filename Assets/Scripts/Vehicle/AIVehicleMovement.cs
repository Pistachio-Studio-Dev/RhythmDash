// [written by Xinyi]

using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

public class AIVehicleMovement : VehicleMovement
{
    private enum AIState
    {
        Inactive,
        Racing,
        Knockback
    }

    private AIState currentState = AIState.Inactive;
    private float stateElapsed = 0f;

    [Header("Spline Following")]
    public SplineContainer spline;
    public float trackHalfWidth = 2f;

    [Header("Lateral Variance")]
    public float lateralChangeSpeed = 0.5f;
    public float lateralNoiseScale = 1f;

    [Header("Speed Variance")]
    public float baseSpeedMultiplier = 1f;
    public float speedNoiseScale = 0.2f;
    public float speedVarianceAmount = 0.1f;
    public float speedChangeRate = 5f;

    [Header("Steering")]
    public float lookAheadDistance = 5f;
    public float maxYawRate = 90f;

    [Header("Rubber Band Speed")]
    public float behindPlayerSpeedMultiplier = 1.2f;
    public float aheadOfPlayerSpeedMultiplier = 0.85f;
    public float rubberBandThreshold = 0.05f;
    public float rubberBandBlendSpeed = 1.5f;

    [Header("Player Reference")]
    public VehicleProgress playerProgress;

    [Header("Knockback")]
    public float knockbackDuration = 1.0f;
    public float normalKnockbackForce = 6f;
    public float starKnockbackForce = 14f;
    public float upwardKnockbackForce = 1.0f;
    public float ignorePlayerCollisionTime = 0.35f;
    private Vector3 pendingKnockbackDirection = Vector3.zero;
    private Collider[] myColliders;

    private float t;
    private float lateralOffset;
    private float noiseOffset;
    private float speedNoiseOffset;
    private float targetSpeed;
    private float currentSpeedMultiplier = 0f;
    private Vector3 desiredVelocity;
    private float desiredMultiplier;
    private VehicleProgress aiProgress;

    public bool IsActive => currentState != AIState.Inactive;
    public bool IsRacing => currentState == AIState.Racing;
    public bool IsInKnockback => currentState == AIState.Knockback;
    public float TargetSpeed => targetSpeed;

    protected override void Start()
    {
        base.Start();
        noiseOffset = Random.Range(0f, 100f);
        speedNoiseOffset = Random.Range(0f, 100f);
        targetSpeed = 0;
        currentSpeedMultiplier = 1f;
        aiProgress = GetComponent<VehicleProgress>();
        myColliders = GetComponentsInChildren<Collider>();
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);

        if (newState == SessionState.Racing)
        {
            EnterState(AIState.Racing);
        }
        else if (newState != SessionState.Finished)
        {
            EnterState(AIState.Inactive);
        }
    }

    protected override void OnVehicleFinished(VehicleProgress vp)
    {
        base.OnVehicleFinished(vp);

        if (vp.transform == transform)
        {
            EnterState(AIState.Inactive);
        }
    }

    private void EnterState(AIState state)
    {
        if (currentState == state)
            return;
        ExitState(currentState);
        currentState = state;
        stateElapsed = 0f;
        switch (state)
        {
            case AIState.Inactive:
                throttleInput = 0f;
                steerInput = 0f;
                inSprint = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                break;

            case AIState.Racing:
                break;

            case AIState.Knockback:
                throttleInput = 0f;
                steerInput = 0f;
                inSprint = false;
                targetSpeed = 0f;
                ApplyPendingKnockback();
                break;
        }
    }

    private void ExitState(AIState state)
    {
    }

    protected override void UpdateInput()
    {
        stateElapsed += Time.fixedDeltaTime;

        switch (currentState)
        {
            case AIState.Inactive:
                UpdateInactive();
                break;

            case AIState.Racing:
                UpdateRacing();
                break;

            case AIState.Knockback:
                UpdateKnockback();
                break;
        }
    }

    private void UpdateInactive()
    {
        throttleInput = 0f;
        steerInput = 0f;
        inSprint = false;
    }

    private void UpdateRacing()
    {
        if (spline == null)
            return;

        UpdateRubberBandMultiplier();
        AdvanceSplinePosition();
        UpdateLateralOffset();
        ComputeTargetSpeed();
        ApplyDesiredVelocity();

        throttleInput = Mathf.Clamp01(targetSpeed / Mathf.Max(autoMaxSpeed, 0.001f));
        inSprint = targetSpeed > autoMaxSpeed;
        steerInput = 0f;

        // Debug.Log($"[AI Speed] targetSpeed={targetSpeed:F2} multiplier={currentSpeedMultiplier:F2} inSprint={inSprint}");
    }

    private void UpdateKnockback()
    {
        throttleInput = 0f;
        steerInput = 0f;
        inSprint = false;
        targetSpeed = 0;

        if (stateElapsed >= knockbackDuration)
        {
            EnterState(AIState.Racing);
        }
    }

    private void UpdateRubberBandMultiplier()
    {
        if (playerProgress == null || aiProgress == null)
        {
            currentSpeedMultiplier = Mathf.MoveTowards(currentSpeedMultiplier, 1f, rubberBandBlendSpeed * Time.fixedDeltaTime);
            return;
        }

        float playerContinuousProgress = playerProgress.CurrentLap +
            (float)playerProgress.PreviousCheckpointIndex / playerProgress.CheckpointCount;

        float aiContinuousProgress = aiProgress.CurrentLap + t;

        float diff = aiContinuousProgress - playerContinuousProgress;

        if (diff > rubberBandThreshold)
        {
            if (desiredMultiplier != aheadOfPlayerSpeedMultiplier)
                Debug.Log("Enemy getting ahead");
            desiredMultiplier = aheadOfPlayerSpeedMultiplier;
        }
        else if (diff < -rubberBandThreshold)
        {
            if (desiredMultiplier != behindPlayerSpeedMultiplier)
                Debug.Log("Enemy getting behind");
            desiredMultiplier = behindPlayerSpeedMultiplier;
        }
        else
        {
            desiredMultiplier = 1f;
        }

        currentSpeedMultiplier = Mathf.MoveTowards(
            currentSpeedMultiplier,
            desiredMultiplier,
            rubberBandBlendSpeed * Time.fixedDeltaTime
        );
    }

    private void AdvanceSplinePosition()
    {
        float splineLength = spline.CalculateLength();
        if (splineLength <= 0f)
            return;

        float physicalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        t += (physicalSpeed / splineLength) * Time.fixedDeltaTime;
        t %= 1f;
    }

    private void UpdateLateralOffset()
    {
        float noiseValue = Mathf.PerlinNoise(t * lateralNoiseScale * 10f + noiseOffset, 0f);
        float targetOffset = (noiseValue * 2f) - 1f;

        lateralOffset = Mathf.MoveTowards(
            lateralOffset,
            targetOffset,
            lateralChangeSpeed * Time.fixedDeltaTime
        );

        lateralOffset = Mathf.Clamp(lateralOffset, -1f, 1f);
    }

    private void ComputeTargetSpeed()
    {
        float baseSpeed = autoMaxSpeed * baseSpeedMultiplier;
        if (playerProgress.HasFinished)
        {
            baseSpeed *= 2f;
        }

        float noiseValue = Mathf.PerlinNoise(GameplayClock.Time * speedNoiseScale + speedNoiseOffset, 0f);
        float noiseDelta = (noiseValue * 2f - 1f) * speedVarianceAmount * baseSpeed;
        float desiredSpeed = Mathf.Clamp(baseSpeed * currentSpeedMultiplier + noiseDelta, autoMaxSpeed * 0.5f, sprintMaxSpeed);

        targetSpeed = Mathf.MoveTowards(
            targetSpeed,
            desiredSpeed,
            speedChangeRate * Time.fixedDeltaTime
        );
    }

    private void ApplyDesiredVelocity()
    {
        float splineLength = spline.CalculateLength();
        float lookAheadT = (t + lookAheadDistance / Mathf.Max(splineLength, 0.001f)) % 1f;

        Vector3 splinePos = spline.EvaluatePosition(lookAheadT);
        Vector3 tangent = ((Vector3)spline.EvaluateTangent(lookAheadT)).normalized;
        Vector3 up = spline.EvaluateUpVector(lookAheadT);
        Vector3 right = Vector3.Cross(tangent, up).normalized;

        Vector3 targetPos = splinePos + right * (lateralOffset * trackHalfWidth);
        Vector3 toTarget = targetPos - transform.position;
        Vector3 desiredForward = Vector3.ProjectOnPlane(toTarget, transform.up).normalized;

        if (desiredForward.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(desiredForward, transform.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                maxYawRate * Time.fixedDeltaTime
            );
        }

        desiredVelocity = transform.forward * targetSpeed;
        Vector3 current = rb.linearVelocity;
        rb.linearVelocity = new Vector3(desiredVelocity.x, current.y, desiredVelocity.z);
    }

    private void ApplyPendingKnockback()
    {
        if (pendingKnockbackDirection.sqrMagnitude < 0.0001f)
            return;

        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.angularVelocity = Vector3.zero;

        Vector3 force = pendingKnockbackDirection + Vector3.up * upwardKnockbackForce;
        rb.AddForce(force, ForceMode.VelocityChange);

        pendingKnockbackDirection = Vector3.zero;
    }

    private IEnumerator TemporarilyIgnorePlayerCollision(GameObject playerObject)
    {
        Collider[] playerColliders = playerObject.GetComponentsInChildren<Collider>();
        if (playerColliders == null || myColliders == null)
            yield break;

        foreach (Collider myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (Collider playerCol in playerColliders)
            {
                if (playerCol == null) continue;
                Physics.IgnoreCollision(myCol, playerCol, true);
            }
        }

        yield return GameplayClock.WaitForSeconds(ignorePlayerCollisionTime);

        foreach (Collider myCol in myColliders)
        {
            if (myCol == null) continue;
            foreach (Collider playerCol in playerColliders)
            {
                if (playerCol == null) continue;
                Physics.IgnoreCollision(myCol, playerCol, false);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsInKnockback) return;

        bool hitPlayer = collision.transform.CompareTag("Player") ||
                        collision.transform.root.CompareTag("Player");

        if (!hitPlayer)
            return;

        GameObject playerObject = collision.transform.root.CompareTag("Player")
            ? collision.transform.root.gameObject
            : collision.transform.gameObject;

        Vector3 away = transform.position - playerObject.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = transform.right;
        away.Normalize();

        PlayerAnimationController playerAnim = playerObject.GetComponent<PlayerAnimationController>();

        bool playerHasStar = playerAnim != null && playerAnim.hasStarPower;
        float knockbackForce = playerHasStar ? starKnockbackForce : normalKnockbackForce;

        pendingKnockbackDirection = away * knockbackForce;
        EnterState(AIState.Knockback);

        StartCoroutine(TemporarilyIgnorePlayerCollision(playerObject));
    }
}