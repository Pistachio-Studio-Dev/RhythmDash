// [ Written by Jiayi ]

using Event;
using UnityEngine;
using UnityEngine.UI;

public class EdgeGlow : MonoBehaviour
{
    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropIntensity = Shader.PropertyToID("_Intensity");
    private static readonly int PropEdgeWidth = Shader.PropertyToID("_EdgeWidth");
    private static readonly int PropSoftness = Shader.PropertyToID("_Softness");
    [SerializeField] private GlowSettings settings;

    private Canvas canvas;
    private float currentIntensity;
    private GlowPhase glowPhase;

    private float glowTimer;
    private Material material;
    private RawImage overlay;

    private void Awake()
    {
        CreateOverlay();
    }

    private void Update()
    {
        AnimateGlow();
        ApplyMaterial();
    }

    private void OnEnable()
    {
        EventManager.StartListening<Beat>(TriggerGlow);
    }

    private void OnDisable()
    {
        EventManager.StopListening<Beat>(TriggerGlow);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
        if (canvas != null)
            Destroy(canvas.gameObject);
    }

    private void TriggerGlow()
    {
        glowPhase = GlowPhase.FadeIn;
        glowTimer = 0f;
    }

    private void AnimateGlow()
    {
        if (glowPhase == GlowPhase.Idle)
            return;

        glowTimer += Time.deltaTime;

        if (glowPhase == GlowPhase.FadeIn)
        {
            var t = Mathf.Clamp01(glowTimer / Mathf.Max(settings.fadeInDuration, 0.001f));
            currentIntensity = t * t * settings.maxIntensity;

            if (t >= 1f)
            {
                glowPhase = GlowPhase.FadeOut;
                glowTimer = 0f;
                currentIntensity = settings.maxIntensity;
            }
        }
        else if (glowPhase == GlowPhase.FadeOut)
        {
            var t = Mathf.Clamp01(glowTimer / Mathf.Max(settings.fadeOutDuration, 0.001f));
            currentIntensity = settings.fadeOutCurve.Evaluate(t) * settings.maxIntensity;

            if (t >= 1f)
            {
                glowPhase = 0;
                currentIntensity = 0f;
            }
        }
    }


    private void ApplyMaterial()
    {
        if (material == null) return;

        material.SetColor(PropColor, settings.glowColor);
        material.SetFloat(PropIntensity, currentIntensity);
        material.SetFloat(PropEdgeWidth, settings.edgeWidth);
        material.SetFloat(PropSoftness, settings.softness);
    }

    private void CreateOverlay()
    {
        var canvasGO = new GameObject("_EdgeGlowCanvas");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var imgGO = new GameObject("EdgeGlowImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        overlay = imgGO.AddComponent<RawImage>();
        overlay.raycastTarget = false;

        var rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        material = new Material(settings.glowShader);
        overlay.material = material;

        material.SetFloat(PropIntensity, 0f);
    }

    private enum GlowPhase
    {
        Idle,
        FadeIn,
        FadeOut
    }
}