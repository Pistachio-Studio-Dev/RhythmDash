// [ Written by Lindsey ]

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Event;
using UnityEngine.UI;

public class LeaderboardManager : SessionBasedBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    private List<VehicleProgress> leaderboard = new List<VehicleProgress>();
    private TMP_Text leaderboardTMP;
    public Transform content;
    public GameObject leaderboardEntryPrefab;
    Color color2 = new Color(81/255f, 84/255f, 89/255f, 0.8f);
    Color color1 = new Color(130/255f, 110/255f, 83/255f, 0.8f);
    private bool allowContinue = false;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
        leaderboardTMP = GetComponentsInChildren<TMP_Text>()[1];
    }

    private void Update()
    {
        if (allowContinue && Input.anyKeyDown && !PauseManager.Instance.IsPaused)
        {
            PauseManager.Instance.Pause();
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        EventManager.StartListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
        EventManager.StartListening<LevelLoaded, string>(OnLevelLoaded);
    }

    protected override void OnDisable() {
        base.OnDisable();
        EventManager.StopListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
        EventManager.StopListening<LevelLoaded, string>(OnLevelLoaded);
    }

    private void OnLevelLoaded(string level)
    {
        allowContinue = false;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
    }

    private void OnVehicleFinished(VehicleProgress vp) {
        if (leaderboard.Exists(existingVP => existingVP.transform.name == vp.transform.name)) {
            return;
        }
        leaderboard.Add(vp);
        leaderboard.Sort((vp1, vp2) => vp1.RaceDuration.CompareTo(vp2.RaceDuration));
        if (currentSessionState == SessionState.Finished && !vp.isPlayer) {
            UpdateLeaderboardText();
        }
    }
    
    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState) {
        base.OnSessionStateChanged(prevState, newState);

        if (newState == SessionState.Finished) {
            StartCoroutine(ShowLeaderboard());
        } else if (newState == SessionState.Lobby) {
            leaderboard.Clear();
            foreach (Transform child in transform) {
                child.gameObject.SetActive(false);
            }
        }
    }
    
    protected override void OnGameplayPauseStateChanged(bool paused) {
        base.OnGameplayPauseStateChanged(paused);
        
        if (currentSessionState == SessionState.Finished) {
            if (paused) {
                foreach (Transform child in transform) {
                    child.gameObject.SetActive(false);
                }
            } else {
                foreach (Transform child in transform) {
                    child.gameObject.SetActive(true);
                }
            }
        }
    }

    private IEnumerator ShowLeaderboard() {
        yield return GameplayClock.WaitForSeconds(2f);
        foreach (Transform child in transform) {
            child.gameObject.SetActive(true);
        }
        UpdateLeaderboardText();
        allowContinue = true;
    }

    private void UpdateLeaderboardText() {
        foreach (Transform child in content) {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < leaderboard.Count; i++) {
            GameObject entry = Instantiate(leaderboardEntryPrefab, content);

            TMP_Text placeText = entry.transform.Find("PlaceText").GetComponent<TMP_Text>();
            TMP_Text nameText = entry.transform.Find("NameText").GetComponent<TMP_Text>();
            TMP_Text timeText = entry.transform.Find("TimeText").GetComponent<TMP_Text>();

            placeText.text = $"{i + 1}";
            nameText.text = leaderboard[i].name;
            timeText.text = FormatTime(leaderboard[i].RaceDuration);

            Canvas.ForceUpdateCanvases();
            Image bg = entry.GetComponent<Image>();
            bg.color = (i % 2 == 0) ? color1 : color2;

            RectTransform rt = entry.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(700, 30);
            entry.transform.SetAsLastSibling();
        }
    }

    string FormatTime(float time) {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return minutes.ToString("00") + ":" + seconds.ToString("00.00");
    }
}
