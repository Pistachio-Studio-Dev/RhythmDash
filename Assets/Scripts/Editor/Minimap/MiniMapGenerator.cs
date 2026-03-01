using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MiniMapGenerator
{
    private const string MenuRoot = "Tools/Minimap/";
    private const string AssetFolder = "Assets/Configs/Minimap";
    private const string PreviewFolder = AssetFolder + "/Previews";
    private const int DefaultPreviewResolution = 2048;
    private const float CapturePaddingWorldUnits = 12f;

    [MenuItem(MenuRoot + "Open Generator")]
    public static void OpenWindow()
    {
        MiniMapGeneratorWindow.Open();
    }

    public static MiniMapSettings GetOrCreateSettingsAsset()
    {
        EnsureAssetFolders();
        string assetPath = GetSettingsAssetPath();
        MiniMapSettings settings = AssetDatabase.LoadAssetAtPath<MiniMapSettings>(assetPath);

        if (settings != null)
            return settings;

        settings = ScriptableObject.CreateInstance<MiniMapSettings>();
        AssetDatabase.CreateAsset(settings, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return settings;
    }

    public static MiniMapTrackData GetOrCreateTrackDataAsset(string sceneName)
    {
        EnsureAssetFolders();
        string assetPath = GetTrackAssetPath(sceneName);
        MiniMapTrackData trackData = AssetDatabase.LoadAssetAtPath<MiniMapTrackData>(assetPath);

        if (trackData != null)
            return trackData;

        trackData = ScriptableObject.CreateInstance<MiniMapTrackData>();
        AssetDatabase.CreateAsset(trackData, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return trackData;
    }

    public static MiniMapTrackData GenerateTrackDataForActiveScene(MiniMapSettings settings, MiniMapTrackData trackData = null)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("MiniMapGenerator: No active loaded scene.");
            return null;
        }

        trackData ??= GetOrCreateTrackDataAsset(scene.name);
        MiniMapTrackSourceResult result = MiniMapTrackSource.BuildFromAuthoring(settings, trackData);
        if (result == null || !result.IsValid)
        {
            Debug.LogError(
                $"MiniMapGenerator: Failed to bake minimap data for scene '{scene.name}'. " +
                "Capture a top-down map and draw the route first.");
            return null;
        }

        Undo.RecordObject(trackData, "Bake MiniMap Track Data");
        trackData.CopyFrom(result, scene.name);
        EditorUtility.SetDirty(trackData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return trackData;
    }

    public static MiniMapTrackData LoadTrackDataAsset(string sceneName)
    {
        return AssetDatabase.LoadAssetAtPath<MiniMapTrackData>(GetTrackAssetPath(sceneName));
    }

    public static Texture2D LoadPreviewTexture(MiniMapTrackData trackData)
    {
        if (trackData == null || !trackData.HasAuthoringPreview)
            return null;

        return AssetDatabase.LoadAssetAtPath<Texture2D>(trackData.authoringCapture.previewTextureAssetPath);
    }

    public static Texture2D CaptureTopDownPreviewForActiveScene(MiniMapTrackData trackData, int resolution = DefaultPreviewResolution)
    {
        if (trackData == null)
        {
            Debug.LogError("MiniMapGenerator: Track data asset is required for preview capture.");
            return null;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("MiniMapGenerator: No active loaded scene.");
            return null;
        }

        if (!MiniMapTrackSource.TryCalculateAuthoringCapture(out MiniMapAuthoringCaptureData captureData, CapturePaddingWorldUnits))
        {
            Debug.LogError("MiniMapGenerator: Could not determine scene bounds for top-down capture.");
            return null;
        }

        EnsureAssetFolders();
        captureData.previewResolution = Mathf.Max(512, resolution);
        captureData.previewTextureAssetPath = GetPreviewAssetPath(scene.name);

        Texture2D capturedTexture = RenderTopDownPreview(captureData);
        if (capturedTexture == null)
        {
            Debug.LogError("MiniMapGenerator: Failed to render top-down preview texture.");
            return null;
        }

        try
        {
            string absolutePath = Path.GetFullPath(captureData.previewTextureAssetPath);
            File.WriteAllBytes(absolutePath, capturedTexture.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(capturedTexture);
        }

        AssetDatabase.ImportAsset(captureData.previewTextureAssetPath, ImportAssetOptions.ForceUpdate);
        ConfigurePreviewImporter(captureData.previewTextureAssetPath);

        Undo.RecordObject(trackData, "Capture Top-Down MiniMap Preview");
        trackData.authoringCapture = captureData;
        EditorUtility.SetDirty(trackData);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return LoadPreviewTexture(trackData);
    }

    public static void SeedTrackFromCheckpoints(MiniMapTrackData trackData)
    {
        if (trackData == null || !trackData.HasAuthoringPreview)
            return;

        System.Collections.Generic.List<Vector2> seededPoints = MiniMapTrackSource.BuildCheckpointSeed(trackData);
        Undo.RecordObject(trackData, "Seed MiniMap Track From Checkpoints");
        trackData.authoredStrokes ??= new System.Collections.Generic.List<MiniMapAuthoredStroke>();
        trackData.authoredNormalizedTrackPoints ??= new System.Collections.Generic.List<Vector2>();
        trackData.authoredStrokes.Clear();
        trackData.authoredNormalizedTrackPoints.Clear();
        trackData.authoredNormalizedTrackPoints.AddRange(seededPoints);
        if (seededPoints.Count > 1)
        {
            trackData.authoredStrokes.Add(new MiniMapAuthoredStroke
            {
                normalizedPoints = new System.Collections.Generic.List<Vector2>(seededPoints)
            });
        }

        EditorUtility.SetDirty(trackData);
        AssetDatabase.SaveAssets();
    }

    public static MiniMapController InstallInActiveScene(MiniMapSettings settings, MiniMapTrackData trackData)
    {
        if (settings == null || trackData == null)
        {
            Debug.LogError("MiniMapGenerator: Settings and track data are required for scene installation.");
            return null;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject minimapObject = GameObject.Find("MiniMapUI");

        if (minimapObject == null)
        {
            minimapObject = new GameObject("MiniMapUI", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(minimapObject, "Create MiniMapUI");
            SceneManager.MoveGameObjectToScene(minimapObject, scene);
        }

        RectTransform rootRect = minimapObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        Canvas canvas = GetOrAddComponent<Canvas>(minimapObject);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(minimapObject);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAddComponent<GraphicRaycaster>(minimapObject);

        RectTransform safeAreaRoot = GetOrCreateChildRect(rootRect, "SafeAreaRoot");
        ConfigureTopLeftRect(safeAreaRoot);
        GetOrAddComponent<SafeAreaAnchor>(safeAreaRoot.gameObject);

        RectTransform minimapRoot = GetOrCreateChildRect(safeAreaRoot, "MiniMapRoot");
        ConfigureTopLeftRect(minimapRoot);
        GetOrAddComponent<CanvasRenderer>(minimapRoot.gameObject);
        Mask minimapMask = GetOrAddComponent<Mask>(minimapRoot.gameObject);
        minimapMask.showMaskGraphic = true;
        GetOrAddComponent<CanvasGroup>(minimapRoot.gameObject);

        RectTransform contentRoot = GetOrCreateChildRect(minimapRoot, "Content");
        ConfigureStretchRect(contentRoot);

        RectTransform trackRoot = GetOrCreateChildRect(contentRoot, "Track");
        ConfigureStretchRect(trackRoot);
        GetOrAddComponent<CanvasRenderer>(trackRoot.gameObject);
        GetOrAddComponent<MiniMapTrackGraphic>(trackRoot.gameObject);

        RectTransform markerRoot = GetOrCreateChildRect(contentRoot, "Markers");
        ConfigureStretchRect(markerRoot);

        MiniMapController controller = GetOrAddComponent<MiniMapController>(minimapObject);
        controller.EditorAssignCurrentReferences();
        controller.Configure(settings, trackData);
        controller.EditorAssignCurrentReferences();
        controller.EditorRebuild();
        EnsureMarkerComponents(markerRoot);
        controller.EditorAssignCurrentReferences();

        EditorUtility.SetDirty(minimapObject);
        EditorSceneManager.MarkSceneDirty(scene);
        return controller;
    }

    private static void EnsureAssetFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Configs"))
        {
            AssetDatabase.CreateFolder("Assets", "Configs");
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets/Configs", "Minimap");
        }

        if (!AssetDatabase.IsValidFolder(PreviewFolder))
        {
            AssetDatabase.CreateFolder(AssetFolder, "Previews");
        }
    }

    private static string GetSettingsAssetPath()
    {
        return $"{AssetFolder}/MiniMap Settings.asset";
    }

    private static string GetTrackAssetPath(string sceneName)
    {
        return $"{AssetFolder}/{sceneName} MiniMap Track.asset";
    }

    private static string GetPreviewAssetPath(string sceneName)
    {
        return $"{PreviewFolder}/{sceneName} MiniMap Preview.png";
    }

    private static Texture2D RenderTopDownPreview(MiniMapAuthoringCaptureData captureData)
    {
        GameObject cameraObject = new GameObject("MiniMap Preview Camera");
        cameraObject.hideFlags = HideFlags.HideAndDontSave;

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = (captureData.worldMax.y - captureData.worldMin.y) * 0.5f;
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = Mathf.Max(500f, captureData.cameraHeight + 500f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.07f, 0.10f, 1f);
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.useOcclusionCulling = false;
        camera.transform.position = new Vector3(
            (captureData.worldMin.x + captureData.worldMax.x) * 0.5f,
            captureData.cameraHeight,
            (captureData.worldMin.y + captureData.worldMax.y) * 0.5f);
        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        RenderTexture renderTexture = new RenderTexture(captureData.previewResolution, captureData.previewResolution, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 1
        };

        Texture2D outputTexture = new Texture2D(captureData.previewResolution, captureData.previewResolution, TextureFormat.RGBA32, false);
        RenderTexture previousActive = RenderTexture.active;

        try
        {
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            outputTexture.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
            outputTexture.Apply(false, false);
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previousActive;
            renderTexture.Release();
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
        }

        return outputTexture;
    }

    private static void ConfigurePreviewImporter(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    private static RectTransform GetOrCreateChildRect(RectTransform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing is RectTransform existingRect)
        {
            return existingRect;
        }

        GameObject child = new GameObject(childName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
        RectTransform rectTransform = child.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = Undo.AddComponent<T>(gameObject);
        }

        return component;
    }

    private static void ConfigureTopLeftRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void ConfigureStretchRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private static void EnsureMarkerComponents(RectTransform markerRoot)
    {
        if (markerRoot == null)
            return;

        MiniMapMarkerView[] markerViews = markerRoot.GetComponentsInChildren<MiniMapMarkerView>(true);
        for (int i = 0; i < markerViews.Length; i++)
        {
            GetOrAddComponent<CanvasRenderer>(markerViews[i].gameObject);
            GetOrAddComponent<MiniMapMarkerGraphic>(markerViews[i].gameObject);
            markerViews[i].EditorAssignCurrentReferences();
        }
    }
}
