using UnityEngine;

public class SoundController : MonoBehaviour
{
    public static SoundController Instance;

    [SerializeField] private SoundLibrary soundLibrary;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        soundLibrary.Init();
    }

    // 🔊 Play SFX
    public void PlaySFX(SoundType type)
    {
        var data = soundLibrary.GetSound(type);
        if (data == null || data.clip == null) return;

        // Create temp object
        GameObject tempGO = new GameObject($"SFX_{type}");
        tempGO.transform.position = Vector3.zero; // or custom position later

        AudioSource source = tempGO.AddComponent<AudioSource>();
        source.clip = data.clip;
        source.volume = data.volume;
        source.pitch = data.pitch;
        source.loop = false;

        source.Play();

        // Destroy after clip duration + buffer
        Destroy(tempGO, data.clip.length + 0.1f);

    }
}