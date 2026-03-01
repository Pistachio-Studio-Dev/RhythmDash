using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class SafeAreaAnchor : MonoBehaviour
{
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [SerializeField] private Corner anchorCorner = Corner.TopLeft;
    [SerializeField] private Vector2 margin = new Vector2(24f, 24f);

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    public Vector2 Margin
    {
        get => margin;
        set
        {
            margin = value;
            Apply();
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        if (lastSafeArea != Screen.safeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
        {
            Apply();
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    public void SetCorner(Corner newCorner)
    {
        anchorCorner = newCorner;
        Apply();
    }

    public void Apply()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        Rect safeArea = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0)
            return;

        Vector2 min = new Vector2(safeArea.xMin / Screen.width, safeArea.yMin / Screen.height);
        Vector2 max = new Vector2(safeArea.xMax / Screen.width, safeArea.yMax / Screen.height);

        switch (anchorCorner)
        {
            case Corner.TopLeft:
                rectTransform.anchorMin = new Vector2(min.x, max.y);
                rectTransform.anchorMax = new Vector2(min.x, max.y);
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = new Vector2(margin.x, -margin.y);
                break;
            case Corner.TopRight:
                rectTransform.anchorMin = new Vector2(max.x, max.y);
                rectTransform.anchorMax = new Vector2(max.x, max.y);
                rectTransform.pivot = new Vector2(1f, 1f);
                rectTransform.anchoredPosition = new Vector2(-margin.x, -margin.y);
                break;
            case Corner.BottomLeft:
                rectTransform.anchorMin = new Vector2(min.x, min.y);
                rectTransform.anchorMax = new Vector2(min.x, min.y);
                rectTransform.pivot = new Vector2(0f, 0f);
                rectTransform.anchoredPosition = new Vector2(margin.x, margin.y);
                break;
            case Corner.BottomRight:
                rectTransform.anchorMin = new Vector2(max.x, min.y);
                rectTransform.anchorMax = new Vector2(max.x, min.y);
                rectTransform.pivot = new Vector2(1f, 0f);
                rectTransform.anchoredPosition = new Vector2(-margin.x, margin.y);
                break;
        }

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }
}
