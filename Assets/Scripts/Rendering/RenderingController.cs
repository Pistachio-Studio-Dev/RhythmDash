using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class RenderingController : MonoBehaviour
{
    public static RenderingController Instance { get; private set; }
    
    private UniversalRenderPipelineAsset asset;
    private VolumeProfile profile;
    
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing RenderingController Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("RenderingController already exists, skipping initialization.");
            Destroy(gameObject);
        }

        asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (asset == null)
        {
            Debug.LogError("No rendering pipeline asset found.");
            Destroy(gameObject);
        }

        profile = asset.volumeProfile;

        if (profile == null)
        {
            Debug.LogError("No Volume profile asset found.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!profile.TryGet(out lensDistortion))
        {
            Debug.LogError("No lens distortion asset found.");
        }
        
        if (!profile.TryGet(out chromaticAberration))
        {
            Debug.LogError("No chromatic aberration asset found.");
        }
    }

    private void Update()
    {
        if (PlayerCharacterController.Instance == null ||
            PlayerCharacterController.Instance.vehicle == null)
            return;

        float speed = PlayerCharacterController.Instance.vehicle.CurrentSpeed;
        float maxSpeed = PlayerCharacterController.Instance.vehicle.MaxSpeed;
        float ratio = (speed / maxSpeed) * (speed / maxSpeed);
        
        float distortionDesired = Mathf.Lerp(0, -GlobalConfig.Instance.PostProcessDistortionMaxIntensity, ratio);
        lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity.value, distortionDesired, Time.deltaTime * GlobalConfig.Instance.PostProcessBlendSpeed);
        
        float chromaticDesired = Mathf.Lerp(0, GlobalConfig.Instance.PostProcessChromaticAberrationMaxIntensity, ratio);
        chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity.value, chromaticDesired, Time.deltaTime * GlobalConfig.Instance.PostProcessBlendSpeed);
        
        VolumeManager.instance.OnVolumeProfileChanged(profile);
    }
    
    private void OnDisable()
    {
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = 0;
        }

        if (chromaticAberration != null)
        {
            chromaticAberration.intensity.value = 0;
        }
        
        VolumeManager.instance.OnVolumeProfileChanged(profile);
    }
}