using UnityEngine;
using System.Collections; // Potøeba pro IEnumerator

[RequireComponent(typeof(AudioSource))]
public class EnemyAudio : MonoBehaviour
{
    [Header("Idle / Ambient")]
    public AudioClip[] idleSounds;
    public float minIdleTime = 10f;    // Zvıšeno na 10s, aby to nebylo otravné
    public float maxIdleTime = 25f;    // Zvıšeno na 25s

    [Header("Combat Sounds")]
    public AudioClip[] hurtSounds;
    public AudioClip[] attackSounds;
    public AudioClip deathSound;
    public AudioClip dodgeSound;
    public AudioClip blockSound;

    [Header("Movement Sounds")]
    public AudioClip[] footstepSounds;
    public AudioClip moveSound; // Speciálnì pro Slime (klouzání)

    [Header("Settings")]
    [Range(0.8f, 1.2f)]
    public float pitchMin = 0.9f;
    [Range(0.8f, 1.2f)]
    public float pitchMax = 1.1f;

    private AudioSource source;
    private EnemyStats stats;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        stats = GetComponent<EnemyStats>();
    }

    void Start()
    {
        // Spustíme smyèku jen pokud MÁME nìjaké zvuky v poli a pole není prázdné
        if (idleSounds != null && idleSounds.Length > 0)
        {
            StartCoroutine(IdleSoundRoutine());
        }
    }

    // --- BEZPEÈNÉ METODY (Neshodí hru, kdy chybí zvuk) ---

    public void PlayHurt()
    {
        PlayRandomClip(hurtSounds);
    }

    public void PlayAttack()
    {
        PlayRandomClip(attackSounds);
    }

    // Pro Bosse (vybere konkrétní zvuk útoku podle èísla)
    public void PlayAttackSpecific(int index)
    {
        if (attackSounds == null || attackSounds.Length == 0) return;

        // Kontrola, zda index existuje (aby hra nespadla, kdy zadáš špatné èíslo)
        if (index >= 0 && index < attackSounds.Length)
        {
            PlayClip(attackSounds[index]);
        }
    }

    public void PlayDeath()
    {
        // Pokud chybí zvuk smrti, prostì nic nedìlej (ádnı error)
        if (deathSound == null) return;

        // PlayClipAtPoint vytvoøí doèasnı objekt, kterı hraje i po znièení enemy
        AudioSource.PlayClipAtPoint(deathSound, transform.position, 1.0f);
    }

    public void PlayDodge()
    {
        if (dodgeSound != null) PlayClip(dodgeSound);
    }

    public void PlayBlock()
    {
        if (blockSound != null) PlayClip(blockSound);
    }

    public void PlayFootstep()
    {
        // Varianta 1: Náhodné kroky (Skeleton, Giant, Ent)
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            // Pøehrát tišeji (0.5f), aby to nepøeøvávalo boj
            PlayClip(footstepSounds[Random.Range(0, footstepSounds.Length)], 0.5f);
        }
        // Varianta 2: Zvuk pohybu (Slime)
        else if (moveSound != null)
        {
            if (!source.isPlaying) source.PlayOneShot(moveSound, 0.5f);
        }
    }

    // --- INTERNÍ LOGIKA ---

    private void PlayRandomClip(AudioClip[] clips)
    {
        // KONTROLA: Pokud je pole prázdné nebo null, konèíme (ádnı crash)
        if (clips == null || clips.Length == 0) return;

        int randomIndex = Random.Range(0, clips.Length);

        // KONTROLA: Pokud je slot v poli prázdnı (zapomnìl jsi tam dát soubor)
        if (clips[randomIndex] != null)
        {
            PlayClip(clips[randomIndex]);
        }
    }

    // Hlavní metoda pro pøehrání - pøidán parametr 'volumeScale'
    private void PlayClip(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return; // Poslední záchrana

        // Variace vıšky tónu (Pitch)
        source.pitch = Random.Range(pitchMin, pitchMax);

        // Pøehrátí zvuku
        source.PlayOneShot(clip, volumeScale);
    }

    IEnumerator IdleSoundRoutine()
    {
        while (true)
        {
            // 1. Èekáme náhodnou dobu (mezi Min a Max)
            float waitTime = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(waitTime);

            // 2. Podmínky pro pøehrání:
            // - Enemy musí mít statistiky a bıt naivu
            // - Musí existovat zvuky v poli
            // - AudioSource musí bıt aktivní
            if (stats != null && stats.currentHealth > 0 &&
                idleSounds != null && idleSounds.Length > 0 &&
                source.enabled && source.gameObject.activeInHierarchy)
            {
                // Vybereme náhodnı zvuk
                AudioClip clip = idleSounds[Random.Range(0, idleSounds.Length)];

                if (clip != null)
                {
                    // Ztišíme Idle zvuky na 0.6, a jsou jen na pozadí
                    PlayClip(clip, 0.6f);
                }
            }
            else if (stats == null || stats.currentHealth <= 0)
            {
                yield break; // Enemy je mrtvı nebo neexistuje, konèíme smyèku
            }
        }
    }
}