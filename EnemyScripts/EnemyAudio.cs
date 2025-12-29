using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip[] hurtSounds;   // Pole zvukù zranìní (napø. 3 rùzné heky)
    public AudioClip[] attackSounds; // Zvuky útoku (napnutí tìtivy / máchnutí)
    public AudioClip deathSound;     // Zvuk smrti
    public AudioClip footstepSound;  // Zvuk kroku (volitelné)

    [Header("Settings")]
    [Range(0.8f, 1.2f)]
    public float pitchMin = 0.9f;
    [Range(0.8f, 1.2f)]
    public float pitchMax = 1.1f;

    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    // Tuto metodu budeme volat, když dostane damage
    public void PlayHurt()
    {
        if (hurtSounds.Length > 0)
        {
            // Vybere náhodný zvuk z pole (aby to nebylo repetitivní)
            AudioClip clip = hurtSounds[UnityEngine.Random.Range(0, hurtSounds.Length)];
            PlayClip(clip);
        }
    }

    // Tuto metodu zavoláme pøes Animation Event (nebo ze skriptu útoku)
    public void PlayAttack()
    {
        if (attackSounds.Length > 0)
        {
            AudioClip clip = attackSounds[UnityEngine.Random.Range(0, attackSounds.Length)];
            PlayClip(clip);
        }
    }

    public void PlayDeath()
    {
        if (deathSound != null)
        {
            PlayClip(deathSound);
        }
    }

    // Pomocná metoda pro pøehrání s variací
    private void PlayClip(AudioClip clip)
    {
        // Náhodná zmìna výšky tónu (Pitch) - zní to mnohem pøirozenìji
        source.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);

        // PlayOneShot dovolí pøehrát zvuky pøes sebe (neusekne ten pøedchozí)
        source.PlayOneShot(clip);
    }
}