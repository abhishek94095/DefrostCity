using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    public List<SoundData> sounds;

    private Dictionary<SoundType, SoundData> soundDict;

    public void Init()
    {
        soundDict = new Dictionary<SoundType, SoundData>();

        foreach (var sound in sounds)
        {
            if (!soundDict.ContainsKey(sound.soundType))
                soundDict.Add(sound.soundType, sound);
        }
    }

    public SoundData GetSound(SoundType type)
    {
        if (soundDict == null)
            Init();

        soundDict.TryGetValue(type, out var data);
        return data;
    }
}