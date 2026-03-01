using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SoundInfo
{
    public string Name;
    public AudioClip Clip;
    public float Volume;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }
    public SoundSettings Settings;
    
    [SerializeField] AudioSource audioSource;
    
    private Dictionary<string, SoundInfo> sounds;
    
    public void Load()
    {
        sounds = new Dictionary<string, SoundInfo>();
        foreach (var soundInfo in Settings.SoundList)
        {
            sounds[soundInfo.Name] = soundInfo;
        }
    }

    public void Play(string soundName)
    {
        if (!sounds.ContainsKey(soundName))
        {
            Debug.LogWarning($"Sound {soundName} not found in Sound List");
        }
        
        SoundInfo soundInfo = sounds[soundName];
        audioSource.PlayOneShot(soundInfo.Clip, soundInfo.Volume);
    }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Initializing Sound Manager Instance");
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Debug.Log("Sound Manager already exists, skipping initialization.");
            Destroy(gameObject);
        }

        if (audioSource == null)
        {
            Debug.LogError("No AudioSource assigned to Sound Manager");
        }
        
        Load();
    }
}
