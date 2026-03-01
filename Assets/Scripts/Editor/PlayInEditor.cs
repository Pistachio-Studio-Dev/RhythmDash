using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class PlayInEditor
{
    private const string PlayModeZoneName = "ToolbarZonePlayModes";
    private const string PlayModeZoneFallbackName = "ToolbarZonePlayMode";
    private const string LeftAlignZoneName = "ToolbarZoneLeftAlign";
    private const string ButtonName = "PlayFromInitToolbarButton";
    private const string PreviousScenePrefsKey = "PlayFromInitToolbarButton.PreviousScene";
    private const string RestoreScenePrefsKey = "PlayFromInitToolbarButton.RestoreScene";

    private static readonly Type ToolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    private static ScriptableObject cachedToolbar;

    static PlayInEditor()
    {
        EditorApplication.update += CreateToolbarButton;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void CreateToolbarButton()
    {
        if (ToolbarType == null)
        {
            return;
        }

        if (cachedToolbar == null)
        {
            UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            if (toolbars.Length == 0)
            {
                return;
            }

            cachedToolbar = toolbars[0] as ScriptableObject;
        }

        VisualElement root = GetToolbarRoot(cachedToolbar);
        if (root == null)
        {
            cachedToolbar = null;
            return;
        }

        VisualElement zone = FindToolbarZone(root);
        if (zone == null)
        {
            return;
        }

        Button button = zone.Q<Button>(ButtonName);
        if (button == null)
        {
            button = CreateButton();
            zone.Add(button);
        }

        button.SetEnabled(CanStartFromInit());
    }

    private static Button CreateButton()
    {
        Button button = new(StartFromInit)
        {
            name = ButtonName,
            text = "Init"
        };
        
        button.style.marginLeft = 4f;
        button.style.marginRight = 2f;
        button.style.paddingLeft = 8f;
        button.style.paddingRight = 8f;
        button.style.height = 22f;

        return button;
    }

    [MenuItem("Tools/Play In Editor")]
    private static void StartFromInit()
    {
        if (!CanStartFromInit())
        {
            return;
        }

        string initScenePath = Path.Join(GlobalConfig.Instance.ScenePath, GlobalConfig.Instance.InitScene) + ".unity";
        SceneAsset initScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(initScenePath);
        if (initScene == null)
        {
            Debug.LogError($"PlayFromInitToolbarButton: Could not find init scene at '{initScenePath}'.");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        bool shouldRestorePreviousScene =
            !string.IsNullOrEmpty(activeScenePath) &&
            !string.Equals(activeScenePath, initScenePath, StringComparison.OrdinalIgnoreCase);

        EditorPrefs.SetBool(RestoreScenePrefsKey, shouldRestorePreviousScene);
        EditorPrefs.SetString(
            PreviousScenePrefsKey,
            shouldRestorePreviousScene ? activeScenePath : string.Empty);

        EditorSceneManager.OpenScene(initScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        if (!EditorPrefs.GetBool(RestoreScenePrefsKey, false))
        {
            return;
        }

        string previousScenePath = EditorPrefs.GetString(PreviousScenePrefsKey, string.Empty);
        EditorPrefs.SetBool(RestoreScenePrefsKey, false);
        EditorPrefs.SetString(PreviousScenePrefsKey, string.Empty);

        if (string.IsNullOrEmpty(previousScenePath) || !File.Exists(previousScenePath))
        {
            return;
        }

        if (string.Equals(EditorSceneManager.GetActiveScene().path, previousScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
    }

    private static bool CanStartFromInit()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling;
    }

    [MenuItem("Tools/Play In Editor", true)]
    private static bool ValidateStartFromInit()
    {
        return CanStartFromInit();
    }

    private static VisualElement GetToolbarRoot(ScriptableObject toolbar)
    {
        if (toolbar == null)
        {
            return null;
        }

        Type toolbarObjectType = toolbar.GetType();

        FieldInfo rootField = toolbarObjectType.GetField("m_Root", BindingFlags.Instance | BindingFlags.NonPublic);
        if (rootField?.GetValue(toolbar) is VisualElement fieldRoot)
        {
            return fieldRoot;
        }

        PropertyInfo visualTreeProperty = toolbarObjectType.GetProperty(
            "visualTree",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (visualTreeProperty?.GetValue(toolbar) is VisualElement propertyRoot)
        {
            return propertyRoot;
        }

        PropertyInfo rootVisualElementProperty = toolbarObjectType.GetProperty(
            "rootVisualElement",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (rootVisualElementProperty?.GetValue(toolbar) is VisualElement rootVisualElement)
        {
            return rootVisualElement;
        }

        return null;
    }

    private static VisualElement FindToolbarZone(VisualElement root)
    {
        VisualElement zone = root.Q(PlayModeZoneName);
        if (zone != null)
        {
            return zone;
        }

        zone = root.Q(PlayModeZoneFallbackName);
        if (zone != null)
        {
            return zone;
        }

        zone = FindDescendant(root, element =>
            !string.IsNullOrEmpty(element.name) &&
            element.name.IndexOf("PlayMode", StringComparison.OrdinalIgnoreCase) >= 0);
        if (zone != null)
        {
            return zone;
        }

        return root.Q(LeftAlignZoneName);
    }

    private static VisualElement FindDescendant(VisualElement parent, Func<VisualElement, bool> predicate)
    {
        foreach (VisualElement child in parent.Children())
        {
            if (predicate(child))
            {
                return child;
            }

            VisualElement nestedMatch = FindDescendant(child, predicate);
            if (nestedMatch != null)
            {
                return nestedMatch;
            }
        }

        return null;
    }
}
