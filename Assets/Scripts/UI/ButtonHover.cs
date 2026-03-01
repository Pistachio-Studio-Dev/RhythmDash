// [ Written by Jiayi ]

using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    public float hoverScale = 1.2f;
    public float hoverAlphaScale = 1.5f;
    public float duration = 0.2f;

    private Vector3 originalScale;
    private float originalAlpha;
    private Tween scaleTween;
    private Tween fadeTween;
    
    private Image backgroundImage;
    private bool isHovered;
    private bool isSelected;

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        originalScale = transform.localScale;
        if (backgroundImage != null)
        {
            originalAlpha = backgroundImage.color.a;
        }
    }

    void OnDisable()
    {
        isHovered = false;
        isSelected = false;
        scaleTween?.Kill();
        fadeTween?.Kill();
        transform.localScale = originalScale;

        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = originalAlpha;
            backgroundImage.color = color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyState();
    }

    public void OnSelect(BaseEventData eventData)
    {
        isSelected = true;
        ApplyState();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        ApplyState();
    }

    private void ApplyState()
    {
        bool highlight = isHovered || isSelected;
        Vector3 targetScale = highlight ? originalScale * hoverScale : originalScale;
        float targetAlpha = highlight ? originalAlpha * hoverAlphaScale : originalAlpha;

        scaleTween?.Kill();
        scaleTween = transform.DOScale(targetScale, duration);

        fadeTween?.Kill();
        fadeTween = backgroundImage?.DOFade(targetAlpha, duration);
    }
}
