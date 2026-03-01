using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MiniMapController : SessionBasedBehaviour
{
    [SerializeField] private MiniMapSettings settings;
    [SerializeField] private MiniMapTrackData bakedTrackData;

    [Header("Runtime References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private GraphicRaycaster graphicRaycaster;
    [SerializeField] private RectTransform safeAreaRoot;
    [SerializeField] private SafeAreaAnchor safeAreaAnchor;
    [SerializeField] private RectTransform minimapRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Mask minimapMask;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private MiniMapTrackGraphic trackGraphic;
    [SerializeField] private RectTransform markerRoot;

    private readonly List<MiniMapMarkerView> markerViews = new List<MiniMapMarkerView>();
    private MiniMapTrackSourceResult trackData;
    private float nextVehicleRefreshTime;

    private void Awake()
    {
        EnsureUiHierarchy();
        ApplySettingsToUi();
        ApplyLayout();
        RebuildTrackData();
        RefreshMarkers();
        ApplyVisibility(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        EnsureUiHierarchy();
        ApplySettingsToUi();
        ApplyLayout();
        RebuildTrackData();
        RefreshMarkers();
        ApplyVisibility(ShouldBeVisible());
    }

    private void Update()
    {
        if (settings == null)
            return;

        ApplyLayout();

        if (Time.unscaledTime >= nextVehicleRefreshTime)
        {
            RefreshMarkers();
            nextVehicleRefreshTime = Time.unscaledTime + 1f;
        }
    }

    private void LateUpdate()
    {
        if (settings == null || trackData == null || !trackData.IsValid || markerRoot == null)
            return;

        if (!TryGetPlayerTarget(out VehicleProgress playerProgress, out Transform playerTarget))
        {
            trackGraphic.ClearRadarContext();
            return;
        }

        trackGraphic.SetRadarContext(playerTarget.position, playerTarget.forward);
        float pixelRadius = CalculateRadarPixelRadius(markerRoot.rect);

        for (int i = 0; i < markerViews.Count; i++)
        {
            MiniMapMarkerView markerView = markerViews[i];
            if (markerView == null || !markerView.gameObject.activeSelf)
                continue;

            VehicleProgress progress = markerView.Progress;
            if (progress == null)
                continue;

            Vehicle vehicle = markerView.GetVehicle();
            Transform target = vehicle != null ? vehicle.transform : progress.transform;
            bool isPlayerMarker = progress.isPlayer;
            Vector2 anchoredPosition;
            Vector2 projectedForward;
            float visibilityAlpha;

            if (isPlayerMarker)
            {
                anchoredPosition = Vector2.zero;
                projectedForward = Vector2.up;
                visibilityAlpha = 1f;
            }
            else
            {
                Vector2 normalizedOffset = MiniMapProjection.ProjectRadarPoint(
                    playerTarget.position,
                    playerTarget.forward,
                    target.position,
                    settings.radarWorldRadius);

                float distance01 = normalizedOffset.magnitude;
                anchoredPosition = normalizedOffset * pixelRadius;
                projectedForward = MiniMapProjection.ProjectRadarDirection(playerTarget.forward, target.forward);
                visibilityAlpha = EvaluateMarkerVisibilityAlpha(distance01);
            }

            markerView.UpdateView(anchoredPosition, projectedForward, progress.HasFinished, visibilityAlpha, settings);
        }

        trackGraphic.SetCheckpointState(
            playerProgress.PreviousCheckpointIndex,
            playerProgress.NextCheckpointIndex,
            playerProgress.HasFinished);
    }

    protected override void OnSessionStateChanged(SessionState prevState, SessionState newState)
    {
        base.OnSessionStateChanged(prevState, newState);
        ApplyVisibility(ShouldBeVisible());
    }

    protected override void OnGameplayPauseStateChanged(bool paused)
    {
        base.OnGameplayPauseStateChanged(paused);
        ApplyVisibility(ShouldBeVisible());
    }

    public void Configure(MiniMapSettings minimapSettings, MiniMapTrackData trackAsset)
    {
        settings = minimapSettings;
        bakedTrackData = trackAsset;
        EnsureUiHierarchy();
        ApplySettingsToUi();
        ApplyLayout();
        RebuildTrackData();
        RefreshMarkers();
    }

#if UNITY_EDITOR
    public void EditorAssignCurrentReferences()
    {
        rootCanvas = GetComponent<Canvas>();
        canvasScaler = GetComponent<CanvasScaler>();
        graphicRaycaster = GetComponent<GraphicRaycaster>();

        safeAreaRoot = transform.Find("SafeAreaRoot") as RectTransform;
        safeAreaAnchor = safeAreaRoot != null ? safeAreaRoot.GetComponent<SafeAreaAnchor>() : null;

        minimapRoot = safeAreaRoot != null ? safeAreaRoot.Find("MiniMapRoot") as RectTransform : null;
        canvasGroup = minimapRoot != null ? minimapRoot.GetComponent<CanvasGroup>() : null;
        minimapMask = minimapRoot != null ? minimapRoot.GetComponent<Mask>() : null;

        contentRoot = minimapRoot != null ? minimapRoot.Find("Content") as RectTransform : null;
        trackGraphic = contentRoot != null ? contentRoot.Find("Track")?.GetComponent<MiniMapTrackGraphic>() : null;
        markerRoot = contentRoot != null ? contentRoot.Find("Markers") as RectTransform : null;

        if (markerRoot != null)
        {
            MiniMapMarkerView[] existingViews = markerRoot.GetComponentsInChildren<MiniMapMarkerView>(true);
            for (int i = 0; i < existingViews.Length; i++)
            {
                existingViews[i].EditorAssignCurrentReferences();
            }
        }

        SyncExistingMarkerViews();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void EditorRebuild()
    {
        EnsureUiHierarchy();
        ApplySettingsToUi();
        ApplyLayout();
        RebuildTrackData();
        RefreshMarkers();
        UnityEditor.EditorUtility.SetDirty(gameObject);
        if (rootCanvas != null)
        {
            UnityEditor.EditorUtility.SetDirty(rootCanvas.gameObject);
        }
    }
#endif

    private void EnsureUiHierarchy()
    {
        RectTransform hostRect = GetComponent<RectTransform>();

        rootCanvas = rootCanvas != null ? rootCanvas : GetComponent<Canvas>();
        if (rootCanvas == null)
        {
            rootCanvas = gameObject.AddComponent<Canvas>();
        }

        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 15;

        canvasScaler = canvasScaler != null ? canvasScaler : GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        graphicRaycaster = graphicRaycaster != null ? graphicRaycaster : GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        hostRect.anchorMin = Vector2.zero;
        hostRect.anchorMax = Vector2.one;
        hostRect.offsetMin = Vector2.zero;
        hostRect.offsetMax = Vector2.zero;
        hostRect.pivot = new Vector2(0.5f, 0.5f);

        if (safeAreaRoot == null)
        {
            safeAreaRoot = CreateChildRect("SafeAreaRoot", hostRect);
            safeAreaAnchor = safeAreaRoot.gameObject.AddComponent<SafeAreaAnchor>();
        }
        else if (safeAreaAnchor == null)
        {
            safeAreaAnchor = safeAreaRoot.GetComponent<SafeAreaAnchor>();
            if (safeAreaAnchor == null)
            {
                safeAreaAnchor = safeAreaRoot.gameObject.AddComponent<SafeAreaAnchor>();
            }
        }

        if (minimapRoot == null)
        {
            minimapRoot = CreateChildRect("MiniMapRoot", safeAreaRoot);
            if (!minimapRoot.TryGetComponent<CanvasRenderer>(out _))
            {
                minimapRoot.gameObject.AddComponent<CanvasRenderer>();
            }
            canvasGroup = minimapRoot.gameObject.AddComponent<CanvasGroup>();
            minimapMask = minimapRoot.gameObject.AddComponent<Mask>();
        }
        else
        {
            if (!minimapRoot.TryGetComponent<CanvasRenderer>(out _))
            {
                minimapRoot.gameObject.AddComponent<CanvasRenderer>();
            }

            canvasGroup = canvasGroup != null ? canvasGroup : minimapRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = minimapRoot.gameObject.AddComponent<CanvasGroup>();
            }

            minimapMask = minimapMask != null ? minimapMask : minimapRoot.GetComponent<Mask>();
            if (minimapMask == null)
            {
                minimapMask = minimapRoot.gameObject.AddComponent<Mask>();
            }
        }

        minimapMask.showMaskGraphic = true;

        if (contentRoot == null)
        {
            contentRoot = CreateChildRect("Content", minimapRoot);
        }

        if (trackGraphic == null)
        {
            RectTransform trackRect = CreateChildRect("Track", contentRoot);
            if (!trackRect.TryGetComponent<CanvasRenderer>(out _))
            {
                trackRect.gameObject.AddComponent<CanvasRenderer>();
            }
            trackGraphic = trackRect.gameObject.AddComponent<MiniMapTrackGraphic>();
        }
        else
        {
            if (!trackGraphic.TryGetComponent<CanvasRenderer>(out _))
            {
                trackGraphic.gameObject.AddComponent<CanvasRenderer>();
            }
        }

        if (markerRoot == null)
        {
            markerRoot = CreateChildRect("Markers", contentRoot);
        }

        SyncExistingMarkerViews();
    }

    private void ApplySettingsToUi()
    {
        if (settings == null)
            return;

        safeAreaAnchor.Margin = settings.safeAreaMargin;
        trackGraphic.ApplySettings(settings);
    }

    private void ApplyLayout()
    {
        if (settings == null || minimapRoot == null || contentRoot == null)
            return;

        float shortEdge = Mathf.Min(Screen.width, Screen.height);
        float height = Mathf.Clamp(shortEdge * settings.panelShortEdgeFactor, settings.minPanelHeight, settings.maxPanelHeight);
        float width = height * settings.panelAspect;

        minimapRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        minimapRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        contentRoot.anchorMin = Vector2.zero;
        contentRoot.anchorMax = Vector2.one;
        contentRoot.offsetMin = Vector2.one * settings.cardPadding;
        contentRoot.offsetMax = Vector2.one * -settings.cardPadding;

        if (trackGraphic != null)
        {
            RectTransform trackRect = trackGraphic.rectTransform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
        }

        if (markerRoot != null)
        {
            markerRoot.anchorMin = Vector2.zero;
            markerRoot.anchorMax = Vector2.one;
            markerRoot.offsetMin = Vector2.zero;
            markerRoot.offsetMax = Vector2.zero;
        }
    }

    private void RebuildTrackData()
    {
        trackData = MiniMapTrackSource.ResolveBaked(bakedTrackData);
        trackGraphic.SetTrackData(trackData);

        if (trackData == null || !trackData.IsValid)
        {
            Debug.LogError(
                $"MiniMapController on '{name}' requires a baked {nameof(MiniMapTrackData)} asset. " +
                "Generate or refresh it from Tools/Minimap before entering play mode.",
                this);
        }
    }

    private void RefreshMarkers()
    {
        SyncExistingMarkerViews();
        VehicleProgress[] progresses = FindObjectsByType<VehicleProgress>(FindObjectsSortMode.None);
        System.Array.Sort(progresses, CompareMarkerPriority);

        for (int i = 0; i < markerViews.Count; i++)
        {
            if (markerViews[i] != null)
            {
                markerViews[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < progresses.Length; i++)
        {
            VehicleProgress progress = progresses[i];
            if (progress == null)
                continue;

            bool isPlayerMarker = progress.isPlayer;
            MiniMapMarkerView markerView = GetOrCreateMarker(i);
            markerView.Bind(progress, isPlayerMarker, settings);

            float size = isPlayerMarker ? settings.playerMarkerSize : settings.aiMarkerSize;
            RectTransform markerRect = markerView.GetComponent<RectTransform>();
            markerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            markerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            markerView.gameObject.SetActive(true);
        }
    }

    private MiniMapMarkerView GetOrCreateMarker(int index)
    {
        while (markerViews.Count <= index)
        {
            RectTransform markerRect = CreateChildRect($"Marker {markerViews.Count}", markerRoot);
            ConfigureMarkerRect(markerRect);
            if (!markerRect.TryGetComponent<CanvasRenderer>(out _))
            {
                markerRect.gameObject.AddComponent<CanvasRenderer>();
            }
            markerRect.gameObject.AddComponent<MiniMapMarkerGraphic>();
            MiniMapMarkerView markerView = markerRect.gameObject.AddComponent<MiniMapMarkerView>();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                markerView.EditorAssignCurrentReferences();
            }
#endif
            markerViews.Add(markerView);
        }

        return markerViews[index];
    }

    private bool ShouldBeVisibleForState(SessionState state)
    {
        if (settings == null)
            return false;

        return state switch
        {
            SessionState.Countdown => settings.showInCountdown,
            SessionState.Racing => settings.showInRacing,
            SessionState.Finished => settings.showInFinished,
            _ => false
        };
    }

    private bool ShouldBeVisible()
    {
        return !isGameplayPaused && ShouldBeVisibleForState(currentSessionState);
    }

    private void ApplyVisibility(bool visible)
    {
        if (canvasGroup == null || minimapRoot == null)
            return;
        
        minimapRoot.gameObject.SetActive(visible);
    }

    private static RectTransform CreateChildRect(string name, RectTransform parent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;
        return rectTransform;
    }

    private void SyncExistingMarkerViews()
    {
        if (markerRoot == null)
            return;

        markerViews.Clear();

        MiniMapMarkerView[] existingViews = markerRoot.GetComponentsInChildren<MiniMapMarkerView>(true);
        System.Array.Sort(existingViews, (left, right) =>
            left.transform.GetSiblingIndex().CompareTo(right.transform.GetSiblingIndex()));

        for (int i = 0; i < existingViews.Length; i++)
        {
            if (existingViews[i] == null)
                continue;

            if (!existingViews[i].TryGetComponent<CanvasRenderer>(out _))
            {
                existingViews[i].gameObject.AddComponent<CanvasRenderer>();
            }
            ConfigureMarkerRect(existingViews[i].GetComponent<RectTransform>());
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                existingViews[i].EditorAssignCurrentReferences();
            }
#endif
            markerViews.Add(existingViews[i]);
        }
    }

    private static int CompareMarkerPriority(VehicleProgress left, VehicleProgress right)
    {
        if (ReferenceEquals(left, right))
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        if (left.isPlayer != right.isPlayer)
            return left.isPlayer ? -1 : 1;

        return string.CompareOrdinal(left.name, right.name);
    }

    private float CalculateRadarPixelRadius(Rect rect)
    {
        return Mathf.Max(0f, Mathf.Min(rect.width, rect.height) * 0.5f - settings.radarEdgePadding);
    }

    private float EvaluateMarkerVisibilityAlpha(float distance01)
    {
        if (distance01 >= 1.15f)
            return 0f;

        if (distance01 <= settings.aiFadeStartNormalized)
            return 1f;

        float t = Mathf.InverseLerp(settings.aiFadeStartNormalized, 1f, distance01);
        return Mathf.Lerp(1f, settings.aiEdgeAlpha, t);
    }

    private static void ConfigureMarkerRect(RectTransform markerRect)
    {
        if (markerRect == null)
            return;

        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
    }

    private bool TryGetPlayerTarget(out VehicleProgress playerProgress, out Transform playerTarget)
    {
        playerProgress = null;
        playerTarget = null;

        for (int i = 0; i < markerViews.Count; i++)
        {
            MiniMapMarkerView markerView = markerViews[i];
            if (markerView == null)
                continue;

            VehicleProgress progress = markerView.Progress;
            if (progress == null || !progress.isPlayer)
                continue;

            Vehicle vehicle = markerView.GetVehicle();
            playerProgress = progress;
            playerTarget = vehicle != null ? vehicle.transform : progress.transform;
            return playerTarget != null;
        }

        return false;
    }
}
