using UnityEngine;

public class TutorialEnemyAnimationController : SessionBasedBehaviour
{
    [SerializeField] private Animator anim;

    private TutorialEnemyMovementController tutorialMovement;
    private float cachedAnimatorSpeed = 1f;

    private void Awake()
    {
        tutorialMovement = GetComponent<TutorialEnemyMovementController>();
        if (tutorialMovement == null)
            tutorialMovement = GetComponentInChildren<TutorialEnemyMovementController>();

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (tutorialMovement == null || anim == null)
        {
            Debug.LogWarning($"[{name}] TutorialEnemyAnimationController expects a TutorialEnemyMovementController and Animator on this object or children.", this);
        }
    }

    private void Update()
    {
        if (isGameplayPaused || anim == null || tutorialMovement == null)
            return;

        anim.SetFloat("speed", tutorialMovement.CurrentSpeed);
        anim.SetBool("isActive", tutorialMovement.IsActive);
        anim.SetBool("isRacing", tutorialMovement.IsRacing);
        anim.SetBool("isInKnockback", tutorialMovement.IsInKnockback);
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        if (anim == null)
            return;

        if (paused)
        {
            cachedAnimatorSpeed = anim.speed;
            anim.speed = 0f;
            return;
        }

        anim.speed = cachedAnimatorSpeed;
    }
}
