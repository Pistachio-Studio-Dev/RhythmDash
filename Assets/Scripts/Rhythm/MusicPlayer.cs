// [ Written by Jiayi ]

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Event;
using UnityEngine;
using UnityEngine.Networking;

public class MusicPlayer : MonoBehaviour
{
    public AudioSource AudioSource;
    public AudioSource AheadAudioSource;
    public bool PlayOnAwake = true;

    private readonly string[] supportAudioFileType = { ".mp3", ".wav", ".ogg" };
    private int currentIndex;
    
    private bool isPlaying = false;
    private bool isInitializing = false;

    private struct MusicInfo
    {
        public string name;
        public string path;
        public AudioClip clip;
    }

    private List<MusicInfo> musicList = new();
    public static MusicPlayer Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing MusicPlayer Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("MusicPlayer already exists, skipping initialization.");
            Destroy(gameObject);
        }

        if (AudioSource == null)
        {
            Debug.LogError("No audio source assigned.");
            Instance = null;
            Destroy(gameObject);
        }

        if (AheadAudioSource == null)
        {
            Debug.LogError("No audio source assigned.");
            Instance = null;
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        StartCoroutine(InitializeAudioFiles());
        if (PlayOnAwake)
        {
            Play();
        }
    }

    public void PrintCurrentMusicName()
    {
        Debug.Log($"Now Playing : ${musicList[currentIndex].name}");
    }

    public void PrintMusicNameList()
    {
        string output = "Loaded Music List:\n";
        for (var i = 0; i < musicList.Count; i++)
            output += $"{i} : {musicList[i].name}\n";
        Debug.Log(output);
    }

    private IEnumerator DoPlay()
    {
        while (isActiveAndEnabled && isPlaying)
        {
            yield return new WaitUntil(() => musicList.Count > 0 && !isInitializing);
            
            var music = musicList[currentIndex];
            
            currentIndex++;
            if (currentIndex >= musicList.Count) currentIndex = 0;
            
            AheadAudioSource.clip = music.clip;
            AheadAudioSource.Play();

            AudioSource.clip = music.clip;
            AudioSource.PlayDelayed(GlobalConfig.Instance.MusicDelayDuration);
            
            Debug.Log($"Start Playing {music.name}");
            EventManager.TriggerEvent<MusicStarted, string>(music.name);
            
            yield return new WaitWhile(() => AudioSource.isPlaying);
        }
    }

    public void Play(int index = 0)
    {
        if (isPlaying)
        {
            Debug.LogWarning($"Music Player already playing {musicList[currentIndex].name}");
            return;
        }
        
        isPlaying = true;
        
        if (index >= 0 && index < musicList.Count)
        {
            currentIndex = index;
        }
        
        StartCoroutine(DoPlay());
    }
    
    public void Play(string musicName)
    {
        if (isPlaying)
        {
            Stop();
        }
        
        isPlaying = true;

        currentIndex = -1;

        for (int i = 0; i < musicList.Count; i++)
        {
            if (musicList[i].name.Contains(musicName))
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex == -1)
        {
            Debug.LogError($"Music Not Found {musicName}");
            currentIndex = 0;
            return;
        }
        
        StartCoroutine(DoPlay());
    }

    public void Stop()
    {
        if (!isPlaying)
        {
            Debug.LogWarning("Music Player not playing");
            return;
        }
        
        isPlaying = false;
        
        StopAllCoroutines();
        AheadAudioSource.Stop();
        AudioSource.Stop();
    }

    public void Next()
    {
        Stop();
        Play();
    }
    
    private IEnumerator InitializeAudioFiles()
    {
        isInitializing = true;
        
        musicList = new List<MusicInfo>();

        var musicPath = GlobalConfig.Instance.MusicPath;
        musicPath = Path.Combine(Application.streamingAssetsPath, musicPath);

        if (Directory.Exists(musicPath))
        {
            var files = Directory.GetFiles(musicPath);

            foreach (var file in files)
            {
                var supported = false;
                foreach (var type in supportAudioFileType)
                    if (file.EndsWith(type))
                        supported = true;

                if (supported)
                {
                    yield return StartCoroutine(LoadAudio(file));
                }
            }
            
            musicList = musicList.OrderBy(x => Random.value).ToList();

            Debug.Log($"Loaded {musicList.Count} music files from {musicPath}:\n {string.Join("\n", musicList.Select(x => x.name).ToArray())}");
        }
        else
        {
            Debug.LogError("Music Folder not found: " + musicPath);
        }
        
        isInitializing = false;
    }

    private IEnumerator LoadAudio(string path)
    {
        var www = UnityWebRequestMultimedia.GetAudioClip($"file://{path}", GetAudioType(path));
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var info = new MusicInfo();
            info.path = path;
            info.name = Path.GetFileNameWithoutExtension(path);
            info.clip = DownloadHandlerAudioClip.GetContent(www);
            musicList.Add(info);
        }
        else
        {
            Debug.LogError("Failed to load: " + www.error);
        }
    }

    private AudioType GetAudioType(string file)
    {
        if (file.EndsWith(".mp3")) return AudioType.MPEG;
        if (file.EndsWith(".wav")) return AudioType.WAV;
        if (file.EndsWith(".ogg")) return AudioType.OGGVORBIS;

        return AudioType.UNKNOWN;
    }
}