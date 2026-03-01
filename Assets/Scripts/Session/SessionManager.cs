// [ Written by Lindsey ]

using System;
using Event;
using UnityEngine;
using System.Collections;


public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance;
    public SessionState PreviousSessionState { get; private set; }
    public SessionState CurrentSessionState { get; private set; } = SessionState.Uninitialized;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        StartSession();
    }

    public void StartSession() {
        SetState(SessionState.Lobby);
        StartCoroutine(StartCountdownRoutine());
    }

    public void EndSession() {
        SetState(SessionState.Uninitialized);
    }

    public void SetState(SessionState newSessionState) {
        PreviousSessionState = CurrentSessionState;
        if (PreviousSessionState != newSessionState) {
            EventManager.TriggerEvent<SessionStateChanged, SessionState, SessionState>(PreviousSessionState, newSessionState);
            CurrentSessionState = newSessionState;
            Debug.Log("[Session Manager] " + PreviousSessionState + " -> " + CurrentSessionState);
        }
    }

    IEnumerator StartCountdownRoutine() {
        yield return null;
        yield return GameplayClock.WaitForSeconds(2f);

        if (CurrentSessionState == SessionState.Lobby)
        {
            SetState(SessionState.Countdown);
        }
    }
}
