using Event;
using System.Collections;
using UnityEngine;

public class TutorialEnemyMovementController : SessionBasedBehaviour
{
    private enum AIState
    {
        Inactive,
        Racing,
        Knockback
    }

    [Header("Tutorial Setup")]
    [SerializeField] private Transform facingReference;

    [Header("Knockback")]
    [SerializeField] private float knockbackDuration = 1.0f;
    [SerializeField] private float normalKnockbackForce = 6f;
    [SerializeField] private float starKnockbackForce = 14f;
    [SerializeField] private float upwardKnockbackForce = 1.0f;
    [SerializeField] private float ignorePlayerCollisionTime = 0.35f;

    private AIState currentState = AIState.Inactive;
    private float stateElapsed = 0f;

    private Rigidbody rb;
    private Collider[] myColliders;
    private Vector3 pendingKnockbackDirection = Vector3.zero;
    private Vector3 idleForward;

    public bool IsActive => currentState != AIState.Inactive;
    public bool IsRacing => currentState == AIState.Racing;
    public bool IsInKnockback => currentState == AIState.Knockback;

    public float CurrentSpeed { get; private set; }
    public float TargetSpeed => 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myColliders = GetComponentsInChildren<Collider>();

        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (facingReference != null)
            idleForward = facingReference.forward;
        else
            idleForward = transform.forward;

        idleForward.y = 0f;
        if (idleForward.sqrMagnitude < 0.0001f)
            idleForward = Vector3.forward;

        idleForward.Normalize();
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);

        if (newState == SessionState.Racing)
            EnterState(AIState.Racing);
        else if (newState != SessionState.Finished)
            EnterState(AIState.Inactive);
    }

    private void Update()
    {
        if (isGameplayPaused)
            return;

        stateElapsed += Time.deltaTime;

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

    private void EnterState(AIState newState)
    {
        if (currentState == newState)
            return;

        ExitState(currentState);
        currentState = newState;
        stateElapsed = 0f;

        switch (currentState)
        {
            case AIState.Inactive:
                CurrentSpeed = 0f;

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                break;

            case AIState.Racing:
                CurrentSpeed = 0f;

                if (rb != null)
                {
                    rb.isKinematic = true;
                }
                break;

            case AIState.Knockback:
                CurrentSpeed = 0f;
                ApplyPendingKnockback();
                break;
        }
    }

    private void ExitState(AIState oldState)
    {
    }

    private void UpdateInactive()
    {
        CurrentSpeed = 0f;
    }

    private void UpdateRacing()
    {
        // Enemy stands still in tutorial.
        // Keep it facing the intended lane direction.
        transform.rotation = Quaternion.LookRotation(idleForward, Vector3.up);

        CurrentSpeed = 0f;

        if (rb != null && rb.isKinematic == false)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void UpdateKnockback()
    {
        if (rb != null)
        {
            Vector3 flatVel = rb.linearVelocity;
            flatVel.y = 0f;
            CurrentSpeed = flatVel.magnitude;
        }
        else
        {
            CurrentSpeed = 0f;
        }

        if (stateElapsed >= knockbackDuration)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            EnterState(AIState.Racing);
        }
    }

    private void ApplyPendingKnockback()
    {
        if (rb == null)
        {
            EnterState(AIState.Racing);
            return;
        }

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (pendingKnockbackDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 force = pendingKnockbackDirection + Vector3.up * upwardKnockbackForce;
            rb.AddForce(force, ForceMode.VelocityChange);
        }

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
        if (currentState == AIState.Knockback)
            return;

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

        EventManager.TriggerEvent<TutorialEnemyHit>();
    }
}