using UnityEngine;
using System;
using UnityEngine.SceneManagement; // Potøeba pro automatickou hudbu

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Lists")]
    public Sound[] musicSounds;
    public Sound[] sfxSounds;

    [System.Serializable]
    public class Sound
    {
        public string name;      // Jméno zvuku (napø. "Jump")
        public AudioClip clip;   // Samotný zvukový soubor
    }
    void Start()
    {
        // Naèteme uloženou hlasitost, pokud neexistuje, dáme 1 (100%)
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1f);

        SetMusicVolume(musicVol);
        SetSFXVolume(sfxVol);

        // Pùvodní kód pro spuštìní hudby v menu:
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "MainMenu") PlayMusic("MainMenu");
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
        PlayerPrefs.SetFloat("MusicVolume", volume); // Uložíme na disk
    }

    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
        PlayerPrefs.SetFloat("SFXVolume", volume); // Uložíme na disk
    }

    // Pomocné metody pro UI, abychom vìdìli, kde má být posuvník pøi zapnutí menu
    public float GetMusicVolume()
    {
        return musicSource.volume;
    }

    public float GetSFXVolume()
    {
        return sfxSource.volume;
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
        }
    }

    // Automatická hudba pøi zmìnì scény
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
            // Vybere náhodnì jednu z denních ambientních skladeb
            // Pokud máš systém den/noc, mùžeš tu dát podmínku
            PlayRandomMusic(new string[] { "VillageDayAmbient1", "VillageDayAmbient2", "VillageDayAmbient3" });
        }
        else if (sceneName == "DungeonScene")
        {
            PlayRandomMusic(new string[] { "DungeonAmbient1", "DungeonAmbient2", "DungeonAmbient3" });
        }
    }

    // --- Pøehrávání Hudby ---
    public void PlayMusic(string name)
    {
        Sound s = Array.Find(musicSounds, x => x.name == name);
        if (s == null) { Debug.LogWarning("Hudba " + name + " nenalezena!"); return; }

        // Pokud už hraje ta samá skladba, nerestartuj ji
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

    // --- Pøehrávání SFX ---
    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        if (s == null) { Debug.LogWarning("SFX " + name + " nenalezen!"); return; }

        // Malá variace výšky tónu (Pitch) - dìlá zvuk pøirozenìjším
        sfxSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);

        sfxSource.PlayOneShot(s.clip);

        // Vrátíme pitch zpátky na 1, aby to neovlivnilo další zvuky trvale
        // (Pozor: u PlayOneShot se pitch aplikuje na source, takže to musíme hlídat,
        // ale pro jednoduchost to u RPG staèí takto, pokud se zvuky nepøekrývají moc rychle)
    }
}