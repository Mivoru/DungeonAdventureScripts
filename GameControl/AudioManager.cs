using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Lists")]
    public Sound[] musicSounds; // "VillageDay", "DungeonAmbient" atd.
    public Sound[] sfxSounds;   // Globální SFX

    [Header("Global Boss Music Pool")]
    public AudioClip[] bossMusicList; // Zásobník pro náhodné bosse

    // Promìnné pro Playlist (Arachne)
    private List<AudioClip> currentPlaylist;
    private bool isPlayingPlaylist = false;

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

        // Reset playlistu pøi naètení scény
        isPlayingPlaylist = false;
        StopAllCoroutines();

        if (sceneName == "MainMenu")
        {
            PlayMusic("MainMenu");
        }
        else if (sceneName == "VillageScene")
        {
            PlayRandomMusic(new string[] { "VillageDayAmbient1", "VillageDayAmbient2" });
        }
        else if (sceneName.Contains("Dungeon") || sceneName == "DungeonScene")
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

    // --- TYTO METODY CHYBÌLY PRO SettingsMenu ---
    public float GetMusicVolume() { return musicSource.volume; }
    public float GetSFXVolume() { return sfxSource.volume; }


    // --- PØEHRÁVÁNÍ KLASICKÉ HUDBY (Jeden track smyèka) ---

    public void PlayMusic(string name)
    {
        if (isPlayingPlaylist)
        {
            isPlayingPlaylist = false;
            StopAllCoroutines();
        }

        Sound s = Array.Find(musicSounds, x => x.name == name);
        if (s == null)
        {
            Debug.LogWarning("Hudba '" + name + "' nenalezena!");
            return;
        }

        if (musicSource.clip == s.clip && musicSource.isPlaying)
        {
            musicSource.loop = true;
            return;
        }

        StartCoroutine(CrossFadeMusic(s.clip, true));
    }

    public void PlayRandomMusic(string[] names)
    {
        if (names.Length == 0) return;
        int randomIndex = UnityEngine.Random.Range(0, names.Length);
        PlayMusic(names[randomIndex]);
    }

    // --- TATO METODA CHYBÌLA PRO BossMusicTrigger ---
    public void PlayBossMusic(AudioClip bossClip)
    {
        // Vypneme playlist, pokud bìží
        isPlayingPlaylist = false;
        StopAllCoroutines();

        if (musicSource.clip == bossClip && musicSource.isPlaying) return;

        StartCoroutine(CrossFadeMusic(bossClip, true));
    }


    // --- PØEHRÁVÁNÍ PLAYLISTU (Arachne / Custom Boss) ---

    public void PlayBossPlaylist(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0) return;

        currentPlaylist = new List<AudioClip>(clips);
        isPlayingPlaylist = true;

        StopAllCoroutines();
        StartCoroutine(PlaylistRoutine());
    }

    IEnumerator PlaylistRoutine()
    {
        int index = 0;

        while (isPlayingPlaylist)
        {
            AudioClip clipToPlay = currentPlaylist[index];

            musicSource.clip = clipToPlay;
            musicSource.loop = false;
            musicSource.Play();

            yield return new WaitForSeconds(clipToPlay.length);

            index++;
            if (index >= currentPlaylist.Count) index = 0;
        }
    }

    // --- PØEHRÁVÁNÍ NÁHODNÉ BOSS HUDBY (Pro ostatní bosse) ---
    public void PlayRandomBossTheme()
    {
        isPlayingPlaylist = false;
        StopAllCoroutines();

        if (bossMusicList == null || bossMusicList.Length == 0) return;

        int randomIndex = UnityEngine.Random.Range(0, bossMusicList.Length);
        AudioClip selectedClip = bossMusicList[randomIndex];

        StartCoroutine(CrossFadeMusic(selectedClip, true));
    }

    // --- FADES ---

    IEnumerator CrossFadeMusic(AudioClip newClip, bool loop)
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
        musicSource.loop = loop;
        musicSource.Play();

        // Fade In
        while (musicSource.volume < targetVolume)
        {
            musicSource.volume += targetVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        musicSource.volume = targetVolume;
    }

    public void PlaySFX(string name)
    {
        Sound s = Array.Find(sfxSounds, x => x.name == name);
        if (s == null) return;

        sfxSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        sfxSource.PlayOneShot(s.clip);
    }
}