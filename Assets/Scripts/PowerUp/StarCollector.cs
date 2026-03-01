using Event;
using UnityEngine;

public class StarCollecter : MonoBehaviour
{
    private void OnTriggerEnter(Collider c)
    {
        if (c.attachedRigidbody == null) return;
        if (!c.attachedRigidbody.CompareTag("Player")) return;
        
        GameObject u = c.attachedRigidbody.gameObject;
        if (u.CompareTag("Player"))
        {
            EventManager.TriggerEvent<CollectPowerUp, GameObject>(gameObject);
            
            PlayerAnimationController anim = u.GetComponent<PlayerAnimationController>();
            if (anim != null)
            {
                anim.ActivateStarPower(GlobalConfig.Instance.PowerUpStarDuration);
                Debug.Log("Player unicorn collected the star powerup.");
            } else {
                Debug.LogWarning("Could not find PlayerAnimationController on player.");
            }
        }
        // else if (u.CompareTag("Enemy"))
        // {
        //     Debug.Log("Enemy unicorncollected the star powerup.");
        // }
        if (SoundManager.Instance != null)
            SoundManager.Instance.Play("CollectedPowerup");
        
        Destroy(Helper.FindRootObjectWithTag(gameObject, "PowerUp"));
    }
}
