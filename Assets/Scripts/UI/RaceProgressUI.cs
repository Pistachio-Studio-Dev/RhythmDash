using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using Event;
using UnityEngine.UI;

public class RaceProgressUI : MonoBehaviour
{
    RectTransform rect;
    [SerializeField] private GameObject banner;
    [SerializeField] private TMP_Text lapTMP;
    Vector2 startPos;
    Vector2 centerPos = new Vector2(0, 0);
    Vector2 endPos;
    private Tween tween;

    private void Start() {
        if (banner != null) {
            rect = banner.GetComponent<RectTransform>();
        }

        if (!rect) {
            Debug.LogError("RaceProgressUI: No RectTransform found on Image component.");
        }

        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }

        float parentWidth = (rect.parent as RectTransform).rect.width;
        centerPos = rect.anchoredPosition;
        startPos = new Vector2(-parentWidth - rect.rect.width, centerPos.y);
        endPos = new Vector2(parentWidth + rect.rect.width, centerPos.y);
        rect.anchoredPosition = startPos;
    }

    private void OnEnable() {
        EventManager.StartListening<VehicleLapFinished, VehicleProgress>(OnVehicleLapFinished);
    }

    private void OnDisable() {
        EventManager.StopListening<VehicleLapFinished, VehicleProgress>(OnVehicleLapFinished);
    }

    private void OnVehicleLapFinished(VehicleProgress vp) {
        if (vp.isPlayer) {
            lapTMP.text = $"Lap {vp.CurrentLap} / {GlobalConfig.Instance.LapsPerSession} complete!";
            Show();
        }
    }

    private void Show() {
        const float duration = 0.3f;
        tween?.Kill();
        tween = DOTween.Sequence()
            .AppendCallback(() =>
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(true);
                }
            })
            .Append(rect.DOAnchorPos(startPos, 0))
            .Append(rect.DOAnchorPos(centerPos, duration))
            .AppendInterval(2f)
            .Append(rect.DOAnchorPos(endPos, duration))
            .AppendCallback(() =>
            {
                foreach (Transform child in transform)
                {
                    child.gameObject.SetActive(false);
                }
            });
    }
}
