// [Written by Elena]
using UnityEngine;

public class AIAnimationController : SessionBasedBehaviour
{
    [SerializeField] private Animator anim;

    private AIVehicleMovement aiMovement;
    private Vehicle vehicle;
    private float cachedAnimatorSpeed = 1f;

    private void Awake()
    {
        aiMovement = GetComponent<AIVehicleMovement>();
        if (aiMovement == null)
            aiMovement = GetComponentInChildren<AIVehicleMovement>();

        vehicle = GetComponent<Vehicle>();
        if (vehicle == null)
            vehicle = GetComponentInChildren<Vehicle>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        
        if (aiMovement == null || vehicle == null || anim == null)
        {
            Debug.LogWarning($"[{name}] AIAnimationController expects an AIVehicleMovement, Vehicle, and Animator on this object or children.", this);
        }
    }

    private void Update()
    {
        if (isGameplayPaused || anim == null || aiMovement == null || vehicle == null)
            return;

        anim.SetFloat("speed", vehicle.CurrentSpeed);

        anim.SetBool("isActive", aiMovement.IsActive);
        anim.SetBool("isRacing", aiMovement.IsRacing);
        anim.SetBool("isInKnockback", aiMovement.IsInKnockback);

        // reserve boosted state for powerups
        // anim.SetBool("isBoosted", aiMovement.collectedSpeedBoost);
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        if (anim == null)
        {
            return;
        }

        if (paused)
        {
            cachedAnimatorSpeed = anim.speed;
            anim.speed = 0f;
            return;
        }

        anim.speed = cachedAnimatorSpeed;
    }
}
