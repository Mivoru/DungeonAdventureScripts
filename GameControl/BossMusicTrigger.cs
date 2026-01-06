using UnityEngine;

public class BossMusicTrigger : MonoBehaviour
{
    public AudioClip bossMusic; // SEM pøetáhni MP3 s boss hudbou
    private bool hasTriggered = false; // Aby se to nespouštìlo víckrát

    void OnTriggerEnter2D(Collider2D other)
    {
        // Pokud už hrála, nebo to není hráè, nic nedìlej
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Vstup do Boss Roomu! Spouštím hudbu.");
            hasTriggered = true;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlayBossMusic(bossMusic);
            }
        }
    }
}