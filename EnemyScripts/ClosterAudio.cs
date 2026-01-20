using System;
using UnityEngine;

public class ClosterAudio : MonoBehaviour
{
    [Header("Audio Sources (Pøetáhni sem komponenty)")]
    public AudioSource sfxSource;
    public AudioSource moveSource;

    [Header("Volume Control (Ovládání hlasitosti)")]
    [Range(0f, 1f)] public float sfxVolume = 0.4f; // Výchozí hlasitost pro efekty
    [Range(0f, 1f)] public float runVolume = 0.2f; // Výchozí hlasitost pro bìh

    [Header("Clips")]
    public AudioClip[] idleSounds;
    public AudioClip biteSound;
    public AudioClip jumpSound;
    public AudioClip runSound;

    [Header("Settings")]
    public float idleIntervalMin = 3f;
    public float idleIntervalMax = 8f;

    private float idleTimer;
    private bool isMoving = false;
    private float startupTimer = 1.5f;
    void Start()
    {
        idleTimer = UnityEngine.Random.Range(idleIntervalMin, idleIntervalMax);

        if (moveSource != null)
        {
            moveSource.clip = runSound;
            moveSource.loop = true;
            moveSource.playOnAwake = false;
            moveSource.volume = runVolume; // <--- Vynutíme hlasitost hned na zaèátku
            moveSource.volume = 0;
        }
    }

    void Update()
    {
        if (startupTimer > 0)
        {
            startupTimer -= Time.deltaTime;
            return; // Dokud neubìhne èas, zbytek Update se neprovede
        }
        // Pojistka: Pokud zmìníš hlasitost bìhu za hry v Inspectoru, hned se projeví
        if (moveSource != null && isMoving)
        {
            moveSource.volume = runVolume;
        }

        // Idle logika
        if (!isMoving && idleSounds.Length > 0)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0)
            {
                PlayRandomIdle();
                idleTimer = UnityEngine.Random.Range(idleIntervalMin, idleIntervalMax);
            }
        }
    }

    // --- Metody volané z AI ---

    public void HandleMovementSound(bool moving, bool charging)
    {
        if (startupTimer > 0) return;
        isMoving = moving;

        if (moveSource == null) return;

        if (moving)
        {
            if (!moveSource.isPlaying)
            {
                moveSource.volume = runVolume; // <--- Vynutit hlasitost pøi startu
                moveSource.Play();
            }

            // Pitch efekt (zrychlení zvuku pøi charge)
            moveSource.pitch = charging ? 1.2f : 0.9f;
        }
        else
        {
            if (moveSource.isPlaying) moveSource.Stop();
        }
    }

    public void PlayJump()
    {
        if (startupTimer > 0) return;
        PlayClip(jumpSound);
    }

    public void PlayAttack()
    {
        if (startupTimer > 0) return;
        PlayClip(biteSound);
    }

    void PlayRandomIdle()
    {
        if (startupTimer > 0) return;
        if (sfxSource != null && sfxSource.isPlaying) return;

        if (idleSounds.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, idleSounds.Length);
            // Idle dáme ještì trochu tišší než ostatní efekty (násobíme 0.7)
            PlayClip(idleSounds[randomIndex], 0.7f);
        }
    }

    // Upravená metoda s Volume Scale
    void PlayClip(AudioClip clip, float loudnessMultiplier = 1f)
    {
        if (startupTimer > 0) return;
        if (clip != null && sfxSource != null)
        {
            sfxSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);

            // TADY JE KOUZLO:
            // PlayOneShot bere (klip, násobiè_hlasitosti).
            // Takže tam pošleme tvoji nastavenou 'sfxVolume'.
            sfxSource.PlayOneShot(clip, sfxVolume * loudnessMultiplier);
        }
    }
}