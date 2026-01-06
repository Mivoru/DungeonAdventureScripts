using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections; // <--- TOTO TI CHYBÌLO PRO IEnumerator!

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Lists")]
    public Sound[] musicSounds;
    public Sound[] sfxSounds;

    [Header("Boss Music Playlist")]
    // SEM v Inspectoru naházej své skladby (BossMusic1, BossMusic2...)
    public AudioClip[] bossMusicList;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        if (sceneName == "MainMenu")
        {
            PlayMusic("MainMenu");
        }
        else if (sceneName == "VillageScene")
        {
            PlayRandomMusic(new string[] { "VillageDayAmbient1", "VillageDayAmbient2" });
        }
        else if (sceneName == "DungeonScene")
        {
            PlayRandomMusic(new string[] { "DungeonAmbient1", "DungeonAmbient2" });
        }
    }

    // --- OVLÁDÁNÍ HLASITOSTI ---
    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume);
    }

    public float GetMusicVolume() { return musicSource.volume; }
    public float GetSFXVolume() { return sfxSource.volume; }


    // --- PØEHRÁVÁNÍ HUDBY ---

    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning("Hudba '" + name + "' nenalezena v poli Music Sounds!");
            return;
        }

        if (musicSource.clip == s.clip && musicSource.isPlaying) return;

        musicSource.clip = s.clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlayRandomMusic(string[] names)
    {
        if (names.Length == 0) return;
        int randomIndex = UnityEngine.Random.Range(0, names.Length);
        PlayMusic(names[randomIndex]);
    }

    // --- BOSS MUSIC LOGIKA (NOVÉ) ---

    // Tuto metodu zavolá BossRoomManager
    public void PlayRandomBossTheme()
    {
        if (bossMusicList == null || bossMusicList.Length == 0)
        {
            Debug.LogWarning("V AudioManageru chybí seznam Boss hudby!");
            return;
        }

        // 1. Vybereme náhodnou skladbu
        int randomIndex = UnityEngine.Random.Range(0, bossMusicList.Length);
        AudioClip selectedClip = bossMusicList[randomIndex];

        // 2. Spustíme ji s pøechodem
        PlayBossMusic(selectedClip);
    }

    public void PlayBossMusic(AudioClip bossClip)
    {
        // Pokud už hraje, nic nedìlej
        if (musicSource.clip == bossClip && musicSource.isPlaying) return;

        StartCoroutine(CrossFadeMusic(bossClip));
    }

    // Tady už to nebude házet chybu, protože máme 'using System.Collections;'
    IEnumerator CrossFadeMusic(AudioClip newClip)
    {
        float fadeDuration = 1.0f;
        float targetVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);

        // Fade Out
        while (musicSource.volume > 0)
        {
            musicSource.volume -= targetVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();

        // Fade In
        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += targetVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        StartCoroutine(FadeOutAndStop());
    }

    IEnumerator FadeOutAndStop()
    {
        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / 1.0f;
            yield return null;
        }
        musicSource.Stop();
        musicSource.volume = startVolume;
    }


    // --- PØEHRÁVÁNÍ SFX ---
    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        if (s == null) { Debug.LogWarning("SFX '" + name + "' nenalezen!"); return; }

        sfxSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(s.clip);
    }
}