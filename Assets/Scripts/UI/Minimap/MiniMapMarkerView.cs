using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(MiniMapMarkerGraphic))]
public class MiniMapMarkerView : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private MiniMapMarkerGraphic markerGraphic;

    private Vehicle trackedVehicle;
    private VehicleProgress trackedProgress;
    private bool isPlayer;
    private Vector2 smoothedPosition;
    private bool hasPosition;

    public VehicleProgress Progress => trackedProgress;

    private void Awake()
    {
        rectTransform ??= GetComponent<RectTransform>();
        markerGraphic ??= GetComponent<MiniMapMarkerGraphic>();
    }

    public void Bind(VehicleProgress vehicleProgress, bool playerMarker, MiniMapSettings settings)
    {
        rectTransform ??= GetComponent<RectTransform>();
        markerGraphic ??= GetComponent<MiniMapMarkerGraphic>();
        trackedProgress = vehicleProgress;
        trackedVehicle = vehicleProgress != null ? vehicleProgress.GetComponent<Vehicle>() : null;
        isPlayer = playerMarker;
        markerGraphic.Configure(playerMarker, settings);
        hasPosition = false;
        gameObject.name = playerMarker ? "PlayerMarker" : $"AIMarker_{vehicleProgress.name}";
    }

    public void UpdateView(Vector2 anchoredPosition, Vector2 forward, bool finished, float visibilityAlpha, MiniMapSettings settings)
    {
        rectTransform ??= GetComponent<RectTransform>();
        markerGraphic ??= GetComponent<MiniMapMarkerGraphic>();

        if (!hasPosition)
        {
            smoothedPosition = anchoredPosition;
            hasPosition = true;
        }
        else
        {
            float lerpFactor = 1f - Mathf.Exp(-settings.markerSmoothing * Time.unscaledDeltaTime);
            smoothedPosition = Vector2.Lerp(smoothedPosition, anchoredPosition, lerpFactor);
        }

        rectTransform.anchoredPosition = smoothedPosition;
        if (isPlayer)
        {
            rectTransform.SetAsLastSibling();
        }

        if (forward.sqrMagnitude > 0.0001f)
        {
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, Vector2.SignedAngle(Vector2.up, forward.normalized));
        }

        markerGraphic.SetFinishedState(finished, settings);
        markerGraphic.SetVisibilityAlpha(visibilityAlpha);
    }

    public Vehicle GetVehicle()
    {
        return trackedVehicle;
    }

#if UNITY_EDITOR
    public void EditorAssignCurrentReferences()
    {
        rectTransform = GetComponent<RectTransform>();
        markerGraphic = GetComponent<MiniMapMarkerGraphic>();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
