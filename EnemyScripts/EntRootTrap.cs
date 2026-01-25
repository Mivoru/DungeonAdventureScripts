using System.Collections;
using UnityEngine;

public class EntRootTrap : MonoBehaviour
{
    [Header("Timing Settings")]
    public float activationDelay = 0.5f;
    public float trapDuration = 2.0f;

    [Header("Combat Settings")]
    public float stunDuration = 1.5f;
    public int damage = 10;

    private bool hasTriggered = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Znièíme objekt po celkové dobì
        Destroy(gameObject, trapDuration);

        // Spustíme odpoèet do aktivace
        StartCoroutine(ActivationRoutine());
    }

    IEnumerator ActivationRoutine()
    {
        // 1. Èekání (Cooldown)
        yield return new WaitForSeconds(activationDelay);

        // 2. Kousnutí!
        CheckCapture();
    }

    void CheckCapture()
    {
        if (hasTriggered) return;

        // Kruhový test kolize
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, LayerMask.GetMask("Player"));

        if (hit != null)
        {
            // Získám PlayerStats (nový centrální mozek)
            PlayerStats stats = hit.GetComponent<PlayerStats>();

            if (stats != null)
            {
                // A) Udìlit damage
                stats.TakeDamage(damage);

                // B) Aplikovat Stun (Zpomalení na 0%)
                Debug.Log("HRÁÈ CHYCEN DO KOØENÙ!");

                // Používáme novou metodu v PlayerStats
                // Parametry: (faktor 0 = úplné zastavení, doba trvání)
                stats.ApplySlowness(0f, stunDuration);
            }

            hasTriggered = true;
        }
    }

    // Vizualizace pro editor (aby jsi vidìl, jak velká je past)
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}