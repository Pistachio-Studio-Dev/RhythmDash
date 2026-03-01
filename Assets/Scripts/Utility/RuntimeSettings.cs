using System.IO;
using UnityEditor;
using UnityEngine;

public class RuntimeSettings
{
    public static RuntimeSettings Instance { get; private set; } = new RuntimeSettings();

    public bool EnableConsole = true;
    public bool EnableTelemetry = true;
    public bool EnableTeleport = true;

    [RuntimeInitializeOnLoadMethod]
    public static void Load()
    {
        string path = GlobalConfig.Instance.RuntimeSettingsPath;
        path = Path.Combine(Application.streamingAssetsPath, path);

        if (!File.Exists(path))
        {
            Debug.LogError($"Runtime settings file not found: {path}");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(path);
            Instance = JsonUtility.FromJson<RuntimeSettings>(json);
            Debug.Log($"Runtime settings loaded successfully.\n{JsonUtility.ToJson(Instance, true)}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load runtime settings from {path}\n{e.Message}");
        }
    }
}
