using UnityEngine;
using System.Collections;

public class PlayerVehicleMovement : VehicleMovement
{
    private float pendingThrottle;
    private float pendingSteer;

    [Header("Wing Boost")]
    private float currentMultiplier = 1f;
    private Coroutine wingBoostRoutine;
    public bool IsWingBoosted => currentMultiplier > 1.01f;
    protected override float SpeedMultiplier => currentMultiplier;

    [Header("Stun")]
    private bool isStunned = false;
    private Coroutine stunRoutine;
    public bool IsStunned => isStunned;

    public void SetInput(float throttle, float steer)
    {
        pendingThrottle = Mathf.Clamp01(throttle);
        pendingSteer    = Mathf.Clamp(steer, -1f, 1f);
    }

    public void ActivateWingBoost(float duration, float multiplier)
    {
        Debug.Log($"ActivateWingBoost called: duration={duration}, multiplier={multiplier}");
        if (wingBoostRoutine != null)
            StopCoroutine(wingBoostRoutine);

        wingBoostRoutine = StartCoroutine(WingBoostRoutine(duration, multiplier));
    }

    private IEnumerator WingBoostRoutine(float duration, float multiplier)
    {
        currentMultiplier = Mathf.Max(1f, multiplier);
        Debug.Log($"Wing boost STARTED. currentMultiplier={currentMultiplier}");

        yield return GameplayClock.WaitForSeconds(duration);

        // clamps speed back to normal range to avoid remaining momentum
        currentMultiplier = 1f;

        Vector3 forward = owner.transform.forward;
        Vector3 v = rb.linearVelocity;

        float forwardSpeed = Vector3.Dot(v, forward);
        float originalTargetMax = Mathf.Lerp(autoMaxSpeed, sprintMaxSpeed, throttleInput);

        if (forwardSpeed > originalTargetMax)
        {
            Vector3 remainder = v - forward * forwardSpeed;
            rb.linearVelocity = remainder + forward * originalTargetMax;
        }
        wingBoostRoutine = null;
    }

        public void ActivateStun(float duration)
    {
        Debug.Log($"ActivateStun called: duration={duration}");

        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

        private IEnumerator StunRoutine(float duration)
    {
        // stuns when player is wing boosted too
        if (wingBoostRoutine != null)
        {
            StopCoroutine(wingBoostRoutine);
            wingBoostRoutine = null;
        }

        currentMultiplier = 1f;
        isStunned = true;
        Debug.Log($"STUN START at {Time.time}");

        // Stop all movement immediately
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        yield return GameplayClock.WaitForSeconds(duration);

        isStunned = false;
        stunRoutine = null;
        Debug.Log($"STUN END at {Time.time}");
    }

    protected override void UpdateInput()
    {
        // Only drive when a player has mounted the vehicle
        if (!owner.isRidden)
        {
            throttleInput = 0f;
            steerInput    = 0f;
            inSprint      = false;
            return;
        }

        // Stun: freeze the unicorn in place
        if (isStunned)
        {
            throttleInput = 0f;
            steerInput = 0f;
            inSprint = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        throttleInput = pendingThrottle;
        steerInput    = pendingSteer;
        inSprint      = throttleInput > 0f;
    }
}