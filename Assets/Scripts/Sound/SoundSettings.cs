using UnityEngine;

[CreateAssetMenu(fileName = "SoundSettings", menuName = "Sound/Sound Settings", order = 0)]
public class SoundSettings : ScriptableObject
{
    public SoundInfo[] SoundList;
}
