using UnityEngine;
using Event;

public class BeatSoundTrigger : MonoBehaviour
{
    private void OnEnable()
    {
        EventManager.StartListening<BeatHitFailed>(OnFailHit);
    }
    
    private void OnDisable()
    {
        EventManager.StopListening<BeatHitFailed>(OnFailHit);
    }
    
    private void OnFailHit()
    {
        if (SessionManager.Instance != null &&
            SessionManager.Instance.CurrentSessionState == SessionState.Racing)
        {
            SoundManager.Instance?.Play("BeatHitFailed");
        }
    }
}
