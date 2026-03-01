using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Event;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Telemetry : MonoBehaviour
{
    public static Telemetry Instance { get; private set; }

    [Serializable]
    private class InputDeviceTelemetry
    {
        public int DeviceId;
        public string Name;
        public string DisplayName;
        public string Layout;
        public string InterfaceName;
        public string Product;
        public string Manufacturer;
    }

    [Serializable]
    private class SceneLoadTelemetry
    {
        public string SceneName;
        public float GameplayTimeSeconds;
    }

    [Serializable]
    private class LapTelemetry
    {
        public string VehicleName;
        public bool IsPlayer;
        public int LapNumber;
        public float LapDurationSeconds;
    }

    [Serializable]
    private class FinishTelemetry
    {
        public int Place;
        public string VehicleName;
        public bool IsPlayer;
        public float RaceDurationSeconds;
    }

    [Serializable]
    private class DiscordWebhookPayload
    {
        public string content;
    }

    [Serializable]
    private class TelemetryData
    {
        public string ProductName;
        public string AppVersion;
        public string UnityVersion;
        public string Platform;
        public string DeviceModel;
        public string DeviceType;
        public string OS;
        public string CPU;
        public int CPUCoreCount;
        public int CPUFrequencyMHz;
        public string GPU;
        public string GraphicsApi;
        public string GraphicsDriver;
        public int RAM;
        public int VRAM;
        public string Display;
        public string StartTime;
        public string QuitTime;
        public float SessionDurationSeconds;
        public float GameplayDurationSeconds;
        public float TotalPausedDurationSeconds;
        public int TotalBeatCount = 0;
        public int CorrectBeatHit = 0;
        public int WrongBeatHit = 0;
        public int BeatAttemptCount = 0;
        public float BeatHitRate = 0f;
        public float BeatSuccessRate = 0f;
        public int PauseCount = 0;
        public int ResumeCount = 0;
        public bool PlayerFinishedRace = false;
        public int PlayerFinishPlace = -1;
        public float PlayerRaceDurationSeconds = -1f;
        public int PlayerLapCount = 0;
        public List<string> PowerUpList = new List<string>();
        public List<string> SessionStateList = new List<string>();
        public List<string> MusicList = new List<string>();
        public List<string> SceneLoadList = new List<string>();
        public List<string> FinishOrderList = new List<string>();
        public List<InputDeviceTelemetry> InputDevice = new List<InputDeviceTelemetry>();
        public List<SceneLoadTelemetry> SceneLoads = new List<SceneLoadTelemetry>();
        public List<LapTelemetry> LapDetails = new List<LapTelemetry>();
        public List<FinishTelemetry> FinishOrder = new List<FinishTelemetry>();
    }

    private readonly TelemetryData telemetryData = new TelemetryData();
    private string telemetryServer;
    private double sessionStartRealtime;
    private double sessionStartEngineTime;
    private double sessionStartGameplayTime;
    private int nextFinishPlace = 1;

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing Telemetry Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("Telemetry already exists, skipping initialization.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (!RuntimeSettings.Instance.EnableTelemetry)
        {
            Debug.Log("Telemetry disabled");
            Destroy(gameObject);
        }
        
        sessionStartRealtime = Time.realtimeSinceStartupAsDouble;
        sessionStartEngineTime = Time.timeAsDouble;
        sessionStartGameplayTime = GameplayClock.TimeAsDouble;
        telemetryServer = GlobalConfig.Instance != null ? GlobalConfig.Instance.DiscordTelemetryServer?.Trim() : null;

        PopulateStaticTelemetry();
        RecordCurrentScene(SceneManager.GetActiveScene().name);
        CaptureInputDevices();
        RegisterListeners();
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        UnregisterListeners();
        Instance = null;
    }

    private void OnApplicationQuit()
    {
        FinalizeTelemetry();

        string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        path = Path.Combine(path, Application.productName);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        path = Path.Combine(path, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json");

        string json = JsonUtility.ToJson(telemetryData, true);
        
#if !UNITY_EDITOR
        File.WriteAllText(path, json);
        TrySendTelemetryToDiscord(json);
#endif

        Debug.Log($"Telemetry file generated at {path}\n{json}");
    }

    private void PopulateStaticTelemetry()
    {
        telemetryData.ProductName = Application.productName;
        telemetryData.AppVersion = Application.version;
        telemetryData.UnityVersion = Application.unityVersion;
        telemetryData.Platform = Application.platform.ToString();
        telemetryData.DeviceModel = SystemInfo.deviceModel;
        telemetryData.DeviceType = SystemInfo.deviceType.ToString();
        telemetryData.OS = SystemInfo.operatingSystem;
        telemetryData.CPU = SystemInfo.processorType;
        telemetryData.CPUCoreCount = SystemInfo.processorCount;
        telemetryData.CPUFrequencyMHz = SystemInfo.processorFrequency;
        telemetryData.GPU = SystemInfo.graphicsDeviceName;
        telemetryData.GraphicsApi = SystemInfo.graphicsDeviceType.ToString();
        telemetryData.GraphicsDriver = SystemInfo.graphicsDeviceVersion;
        telemetryData.RAM = SystemInfo.systemMemorySize;
        telemetryData.VRAM = SystemInfo.graphicsMemorySize;
        telemetryData.Display =
            $"{Screen.currentResolution.width}x{Screen.currentResolution.height} @ {Screen.currentResolution.refreshRateRatio.value:0.##}Hz";
        telemetryData.StartTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    }

    private void CaptureInputDevices()
    {
        foreach (var device in InputSystem.devices)
        {
            AddInputDevice(device);
        }
    }

    private void RegisterListeners()
    {
        EventManager.StartListening<Beat>(OnBeat);
        EventManager.StartListening<BeatHit>(OnBeatHit);
        EventManager.StartListening<BeatHitFailed>(OnBeatHitFailed);
        EventManager.StartListening<Pause>(OnPause);
        EventManager.StartListening<Resume>(OnResume);
        EventManager.StartListening<CollectPowerUp, GameObject>(OnCollectPowerUp);
        EventManager.StartListening<SessionStateChanged, SessionState, SessionState>(OnSessionStateChanged);
        EventManager.StartListening<MusicStarted, string>(OnMusicStarted);
        EventManager.StartListening<VehicleLapFinished, VehicleProgress>(OnVehicleLapFinished);
        EventManager.StartListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void UnregisterListeners()
    {
        EventManager.StopListening<Beat>(OnBeat);
        EventManager.StopListening<BeatHit>(OnBeatHit);
        EventManager.StopListening<BeatHitFailed>(OnBeatHitFailed);
        EventManager.StopListening<Pause>(OnPause);
        EventManager.StopListening<Resume>(OnResume);
        EventManager.StopListening<CollectPowerUp, GameObject>(OnCollectPowerUp);
        EventManager.StopListening<SessionStateChanged, SessionState, SessionState>(OnSessionStateChanged);
        EventManager.StopListening<MusicStarted, string>(OnMusicStarted);
        EventManager.StopListening<VehicleLapFinished, VehicleProgress>(OnVehicleLapFinished);
        EventManager.StopListening<VehicleFinished, VehicleProgress>(OnVehicleFinished);
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnBeat()
    {
        telemetryData.TotalBeatCount++;
    }

    private void OnBeatHit()
    {
        telemetryData.CorrectBeatHit++;
    }

    private void OnBeatHitFailed()
    {
        telemetryData.WrongBeatHit++;
    }

    private void OnPause()
    {
        telemetryData.PauseCount++;
    }

    private void OnResume()
    {
        telemetryData.ResumeCount++;
    }

    private void OnCollectPowerUp(GameObject go)
    {
        telemetryData.PowerUpList.Add($"{GameplayClock.Time:0.000} : {Helper.GetGameObjectPath(go)}");
    }

    private void OnSessionStateChanged(SessionState oldState, SessionState newState)
    {
        telemetryData.SessionStateList.Add($"{GameplayClock.Time:0.000} : {oldState} -> {newState}");
    }

    private void OnMusicStarted(string music)
    {
        telemetryData.MusicList.Add($"{GameplayClock.Time:0.000} : {music}");
    }

    private void OnVehicleLapFinished(VehicleProgress vp)
    {
        telemetryData.LapDetails.Add(new LapTelemetry
        {
            VehicleName = vp.name,
            IsPlayer = vp.isPlayer,
            LapNumber = vp.CurrentLap,
            LapDurationSeconds = vp.CurrentDuration
        });

        if (vp.isPlayer)
        {
            telemetryData.PlayerLapCount = vp.CurrentLap;
        }
    }

    private void OnVehicleFinished(VehicleProgress vp)
    {
        int place = nextFinishPlace++;
        telemetryData.FinishOrderList.Add($"{place}. {vp.name} ({vp.RaceDuration:0.000}s)");
        telemetryData.FinishOrder.Add(new FinishTelemetry
        {
            Place = place,
            VehicleName = vp.name,
            IsPlayer = vp.isPlayer,
            RaceDurationSeconds = vp.RaceDuration
        });

        if (!vp.isPlayer)
        {
            return;
        }

        telemetryData.PlayerFinishedRace = true;
        telemetryData.PlayerFinishPlace = place;
        telemetryData.PlayerRaceDurationSeconds = vp.RaceDuration;
    }

    private void OnActiveSceneChanged(Scene previousScene, Scene newScene)
    {
        RecordCurrentScene(newScene.name);
    }

    private void RecordCurrentScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        telemetryData.SceneLoadList.Add($"{GameplayClock.Time:0.000} : {sceneName}");
        telemetryData.SceneLoads.Add(new SceneLoadTelemetry
        {
            SceneName = sceneName,
            GameplayTimeSeconds = GameplayClock.Time
        });
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added)
        {
            AddInputDevice(device);
        }
    }

    private void AddInputDevice(InputDevice device)
    {
        if (device == null || telemetryData.InputDevice.Exists(info => info.DeviceId == device.deviceId))
        {
            return;
        }

        telemetryData.InputDevice.Add(new InputDeviceTelemetry
        {
            DeviceId = device.deviceId,
            Name = device.name,
            DisplayName = device.displayName,
            Layout = device.layout,
            InterfaceName = device.description.interfaceName,
            Product = device.description.product,
            Manufacturer = device.description.manufacturer
        });
    }

    private void FinalizeTelemetry()
    {
        telemetryData.QuitTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        telemetryData.SessionDurationSeconds = (float)(Time.realtimeSinceStartupAsDouble - sessionStartRealtime);
        telemetryData.GameplayDurationSeconds = (float)(GameplayClock.TimeAsDouble - sessionStartGameplayTime);
        telemetryData.TotalPausedDurationSeconds = (float)Math.Max(
            0d,
            (Time.timeAsDouble - sessionStartEngineTime) - telemetryData.GameplayDurationSeconds
        );
        telemetryData.BeatAttemptCount = telemetryData.CorrectBeatHit + telemetryData.WrongBeatHit;
        telemetryData.BeatHitRate = telemetryData.TotalBeatCount > 0
            ? telemetryData.CorrectBeatHit / (float)telemetryData.TotalBeatCount
            : 0f;
        telemetryData.BeatSuccessRate = telemetryData.BeatAttemptCount > 0
            ? telemetryData.CorrectBeatHit / (float)telemetryData.BeatAttemptCount
            : 0f;
    }

    private void TrySendTelemetryToDiscord(string telemetryMessage)
    {
        if (string.IsNullOrWhiteSpace(telemetryServer))
        {
            return;
        }

        try
        {
            List<string> chunks = SplitPlainTextForDiscord(telemetryMessage, 1800);

            for (int i = 0; i < chunks.Count; i++)
            {
                string prefix = chunks.Count > 1 ? $"Telemetry {i + 1}/{chunks.Count}\n" : string.Empty;
                PostDiscordMessage(prefix + chunks[i]);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to send telemetry to Discord webhook: {ex.Message}");
        }
    }

    private void PostDiscordMessage(string plainTextMessage)
    {
        var request = (HttpWebRequest)WebRequest.Create(telemetryServer);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Timeout = 5000;
        request.ReadWriteTimeout = 5000;

        string payloadJson = JsonUtility.ToJson(new DiscordWebhookPayload
        {
            content = plainTextMessage
        });

        byte[] body = Encoding.UTF8.GetBytes(payloadJson);
        request.ContentLength = body.Length;

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(body, 0, body.Length);
        }

        using (var response = (HttpWebResponse)request.GetResponse())
        {
            if ((int)response.StatusCode >= 400)
            {
                Debug.LogWarning($"Discord webhook returned {(int)response.StatusCode} {response.StatusDescription}");
            }
        }
    }

    private static List<string> SplitPlainTextForDiscord(string text, int maxChunkLength)
    {
        var chunks = new List<string>();

        if (string.IsNullOrEmpty(text))
        {
            chunks.Add(string.Empty);
            return chunks;
        }

        for (int startIndex = 0; startIndex < text.Length; startIndex += maxChunkLength)
        {
            int length = Math.Min(maxChunkLength, text.Length - startIndex);
            chunks.Add(text.Substring(startIndex, length));
        }

        return chunks;
    }
}
