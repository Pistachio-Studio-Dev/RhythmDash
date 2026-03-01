// [ Written by Jiayi ]

using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Event;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NotificationCenter : MonoBehaviour
{
    public static NotificationCenter Instance { get; private set; }
    
    public GameObject Root;
    public GameObject Panel;
    public GameObject Target;
    
    public GameObject RootLong;
    public GameObject PanelLong;
    public GameObject TargetLong;
    
    private TextMeshProUGUI text;
    private RectTransform panelRect;
    private CanvasGroup group;
    private Vector2 targetPosition;
    private Vector2 startPosition;
    
    private TextMeshProUGUI textLong;
    private RectTransform panelRectLong;
    private CanvasGroup groupLong;
    private Vector2 targetPositionLong;
    private Vector2 startPositionLong;
    
    private Queue<string> notificationQueue = new Queue<string>();
    
    private string notificationLong = null;
    private bool notificationLongChanged = false;

    public void ShowNotification(string content)
    {
        notificationQueue.Enqueue(content);
    }
    
    public void ShowLongNotification(string content)
    {
        notificationLongChanged = true;
        notificationLong = content;
    }
    
    public void UpdateLongNotification(string content)
    {
        if (notificationLong == null)
        {
            Debug.LogWarning("No long notification to update. Call ShowLongNotification to display one first.");
            ShowLongNotification(content);
            return;
        }
        
        notificationLong = content;
        textLong.text = notificationLong;
    }
    
    public void HideLongNotification()
    {
        notificationLongChanged = true;
        notificationLong = null;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing NotificationCenter Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("NotificationCenter already exists, skipping initialization.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        text = Panel.GetComponentInChildren<TextMeshProUGUI>();
        panelRect = Panel.GetComponent<RectTransform>();
        startPosition = panelRect.anchoredPosition;
        targetPosition = Target.GetComponent<RectTransform>().anchoredPosition;
        group = Root.GetComponent<CanvasGroup>();
        Root.SetActive(false);
        StartCoroutine(ProcessNotification());
        
        textLong = PanelLong.GetComponentInChildren<TextMeshProUGUI>();
        panelRectLong = PanelLong.GetComponent<RectTransform>();
        startPositionLong = panelRectLong.anchoredPosition;
        targetPositionLong = TargetLong.GetComponent<RectTransform>().anchoredPosition;
        groupLong = RootLong.GetComponent<CanvasGroup>();
        RootLong.SetActive(false);
        StartCoroutine(ProcessLongNotification());
    }

    private IEnumerator ProcessNotification()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitUntil(() => notificationQueue.Count > 0);
            
            float fadeDuration = 0.8f;
            string content = notificationQueue.Dequeue();
        
            Debug.Log($"Show Notification: \"{content}\"");
        
            Root.SetActive(true);
            text.text = content;
        
            yield return DOTween.Sequence()
                .Append(group.DOFade(0, 0))
                .Append(panelRect.DOAnchorPos(targetPosition, fadeDuration))
                .Join(group.DOFade(1, fadeDuration))
                .AppendInterval(GlobalConfig.Instance.NotificationDuration)
                .Append(panelRect.DOAnchorPos(startPosition, fadeDuration))
                .Join(group.DOFade(0, fadeDuration))
                .AppendCallback((() => { Root.SetActive(false); }))
                .WaitForCompletion();
        }
    }

    private IEnumerator ProcessLongNotification()
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitUntil(() => notificationLongChanged);
            notificationLongChanged = false;
            
            float fadeDuration = 0.8f;
            
            if (RootLong.activeSelf)
            {
                Debug.Log("Hide Long Notification");
                
                yield return DOTween.Sequence()
                    .Append(panelRectLong.DOAnchorPos(startPositionLong, fadeDuration))
                    .Join(groupLong.DOFade(0, fadeDuration))
                    .AppendCallback(() => { RootLong.SetActive(false); })
                    .WaitForCompletion();
            }
            
            if (notificationLong != null)
            {
                Debug.Log($"Show Long Notification: \"{notificationLong}\"");
        
                RootLong.SetActive(true);
                textLong.text = notificationLong;
        
                yield return DOTween.Sequence()
                    .Append(groupLong.DOFade(0, 0))
                    .Append(panelRectLong.DOAnchorPos(targetPositionLong, fadeDuration))
                    .Join(groupLong.DOFade(1, fadeDuration))
                    .WaitForCompletion();
            }
        }
    }
}
