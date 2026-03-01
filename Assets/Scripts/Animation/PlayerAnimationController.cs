using UnityEngine;
using System.Collections;

public class PlayerAnimationController : SessionBasedBehaviour
{
    private Animator anim;
    private PlayerVehicleMovement movement;

    private float fallTriggerCooldown = 0.5f;
    private float fallCooldownTimer = 0f;
    private float cachedAnimatorSpeed = 1f;

    private bool starImmnue = false;
    public bool hasStarPower => starImmnue;
    private Coroutine starRoutine;
    private Coroutine wingFxRoutine;
    private Coroutine stunFxRoutine;


    [Header("Powerup FX")]
    [SerializeField] private GameObject fxAnchor;
    [SerializeField] private GameObject starImmunePrefab;
    [SerializeField] private GameObject wingSpeedBoostPrefab;
    [SerializeField] private GameObject stunPrefab;

    private GameObject starFxInstance;
    private GameObject wingFxInstance;
    private GameObject stunFxInstance;

    private void Awake()
    {
        movement = GetComponent<PlayerVehicleMovement>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (movement == null)
            Debug.LogWarning($"[{name}] PlayerAnimationController could not find PlayerVehicleMovement.", this);

        if (anim == null)
            Debug.LogWarning($"[{name}] PlayerAnimationController could not find Animator.", this);    
    }

    private void Update()
    {
        if (isGameplayPaused || anim == null || movement == null)
            return;

        if (fallCooldownTimer > 0f)
            fallCooldownTimer -= Time.deltaTime;

        bool isMoving = movement.CurrentSpeed > 0.01f;
        bool isBoosted = isMoving && movement.InSprint;

        anim.SetBool("isMoving", isMoving);
        anim.SetBool("isBoosted", isBoosted);
    }

    private GameObject SpawnAttachedFx(GameObject fxPrefab)
    {
        if (fxPrefab == null)
        {
            Debug.LogWarning($"[{name}] FX prefab is not assigned.", this);
            return null;
        }

        if (fxAnchor == null)
        {
            Debug.LogWarning($"[{name}] FX anchor is not assigned.", this);
            return null;
        }

        GameObject fx = Instantiate(fxPrefab, fxAnchor.transform);
        fx.transform.localPosition = Vector3.zero;
        fx.transform.localRotation = Quaternion.identity;
        return fx;
    }

    private void ClearFx(ref GameObject fxInstance)
    {
        if (fxInstance != null)
        {
            Destroy(fxInstance);
            fxInstance = null;
        }
    }

    public void ActivateStarPower(float duration)
    {
        if (starRoutine != null)
            StopCoroutine(starRoutine);

        ClearFx(ref starFxInstance);
        starRoutine = StartCoroutine(StarPowerRoutine(duration));
    }

    private IEnumerator StarPowerRoutine(float duration)
    {
        starImmnue = true;
        starFxInstance = SpawnAttachedFx(starImmunePrefab);
        Debug.Log("Star power ON");

        yield return GameplayClock.WaitForSeconds(duration);

        starImmnue = false;
        ClearFx(ref starFxInstance);
        starRoutine = null;
        Debug.Log("Star power OFF");
    }

    public void ActivateWingBoostFx(float duration)
    {
        if (wingFxRoutine != null)
            StopCoroutine(wingFxRoutine);
    
        ClearFx(ref wingFxInstance);
        wingFxRoutine = StartCoroutine(WingBoostFxRoutine(duration));
    }
    
    private IEnumerator WingBoostFxRoutine(float duration)
    {
        wingFxInstance = SpawnAttachedFx(wingSpeedBoostPrefab);
        wingFxInstance.transform.localPosition = Vector3.up * 0.5f;
        Debug.Log("Wing boost ON");
    
        yield return GameplayClock.WaitForSeconds(duration);
    
        ClearFx(ref wingFxInstance);
        wingFxRoutine = null;
        Debug.Log("Wing boost OFF");
    }

    public void ActivateStunFX(float duration)
    {
        if (stunFxRoutine != null)
            StopCoroutine(stunFxRoutine);

        stunFxRoutine = StartCoroutine(StunFxRoutine(duration));
    }

    private IEnumerator StunFxRoutine(float duration)
    {
        if (anim != null)
            anim.SetBool("isStunned", true);

        if (stunFxInstance != null)
            Destroy(stunFxInstance);

        stunFxInstance = SpawnAttachedFx(stunPrefab);
        stunFxInstance.transform.localPosition = new Vector3(0.2f, 1.0f, -0.1f);
        stunFxInstance.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        yield return GameplayClock.WaitForSeconds(duration);

        if (anim != null)
            anim.SetBool("isStunned", false);

        if (stunFxInstance != null)
            Destroy(stunFxInstance);

        stunFxInstance = null;
        stunFxRoutine = null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isGameplayPaused || anim == null || fallCooldownTimer > 0f)
            return;

        if (collision.transform.CompareTag("Player") || collision.transform.root.CompareTag("Enemy") || collision.transform.CompareTag("Enemy"))
        {   
            if (starImmnue)
            {
                Debug.Log("Star power is protecting you! You cannot be knocked over.");
                return;
            }

            anim.ResetTrigger("fall");
            anim.SetTrigger("fall");
            fallCooldownTimer = fallTriggerCooldown;
            // check multiple falldown triggers
            // implement speed theshold for fall down
        }
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
