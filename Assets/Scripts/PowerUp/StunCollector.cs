using UnityEngine;
using Event;

public class StunCollector : MonoBehaviour
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
            movement.ActivateStun(GlobalConfig.Instance.PowerUpStunDuration);

        if (anim != null)
            anim.ActivateStunFX(GlobalConfig.Instance.PowerUpStunDuration);
        
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play("CollectedPowerup");

        Destroy(Helper.FindRootObjectWithTag(gameObject, "PowerUp"));
    }
}