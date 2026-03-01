// [ Written by Lindsey ]

using UnityEngine; 
using Event;

public abstract class SessionBasedBehaviour : MonoBehaviour {
    protected SessionState currentSessionState {get; private set; }
    protected SessionState previousSessionState {get; private set; }
    protected bool isGameplayPaused { get; private set; }

    protected virtual void OnEnable() {
        EventManager.StartListening<SessionStateChanged, SessionState, SessionState>(OnSessionStateChanged);
        EventManager.StartListening<Pause>(HandleGameplayPaused);
        EventManager.StartListening<Resume>(HandleGameplayResumed);
        if (SessionManager.Instance != null) {
            OnSessionStateChanged(SessionManager.Instance.PreviousSessionState, SessionManager.Instance.CurrentSessionState);
        }
        ApplyGameplayPauseState(GameplayClock.IsPaused);
    }

    protected virtual void OnDisable() {
        EventManager.StopListening<Resume>(HandleGameplayResumed);
        EventManager.StopListening<Pause>(HandleGameplayPaused);
        EventManager.StopListening<SessionStateChanged, SessionState, SessionState>(OnSessionStateChanged);
        isGameplayPaused = false;
    }

    protected virtual void OnSessionStateChanged(SessionState prevState, SessionState newState) {
        previousSessionState = prevState;
        currentSessionState = newState;
    }

    protected virtual void OnGameplayPauseStateChanged(bool paused)
    {
    }

    private void HandleGameplayPaused()
    {
        ApplyGameplayPauseState(true);
    }

    private void HandleGameplayResumed()
    {
        ApplyGameplayPauseState(false);
    }

    private void ApplyGameplayPauseState(bool paused)
    {
        if (isGameplayPaused == paused)
        {
            return;
        }

        isGameplayPaused = paused;
        OnGameplayPauseStateChanged(paused);
    }
}
