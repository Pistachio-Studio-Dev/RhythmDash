using Event;
using UnityEngine;

public class WingCollector : MonoBehaviour
{
    private void OnTriggerEnter(Collider c)
    {
        
        if (c.attachedRigidbody == null) return;
        if (!c.attachedRigidbody.CompareTag("Player")) return;
        
        EventManager.TriggerEvent<CollectPowerUp, GameObject>(gameObject);
        
        GameObject u = c.attachedRigidbody.gameObject;
        PlayerVehicleMovement movement = u.GetComponent<PlayerVehicleMovement>();
        PlayerAnimationController anim = u.GetComponent<PlayerAnimationController>();

        if (movement != null)
            // Physics Layer in PlayerVehicleMovement
            movement.ActivateWingBoost(GlobalConfig.Instance.PowerUpWingBoostDuration, GlobalConfig.Instance.PowerUpWingBoostSpeedMultiplier);

        if (anim != null)
            // Animation Layer in PlayerAnimationController
            anim.ActivateWingBoostFx(GlobalConfig.Instance.PowerUpWingBoostDuration);
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play("CollectedPowerup");

        Destroy(Helper.FindRootObjectWithTag(gameObject, "PowerUp"));
    }
}
