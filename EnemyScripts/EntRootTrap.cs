using System.Collections;
using UnityEngine;

public class EntRootTrap : MonoBehaviour
{
    [Header("Timing Settings")]
    public float activationDelay = 0.5f; // <--- NOVÉ: Èas od spawnu do "kousnutí" (Cooldown)
    public float trapDuration = 2.0f;    // Celková životnost objektu (musí být víc než activationDelay)

    [Header("Combat Settings")]
    public float stunDuration = 1.5f;
    public int damage = 10;

    private bool hasTriggered = false;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Znièíme objekt po celkové dobì (úklid)
        Destroy(gameObject, trapDuration);

        // Spustíme odpoèet do aktivace
        StartCoroutine(ActivationRoutine());
    }

    IEnumerator ActivationRoutine()
    {
        // 1. Èekáme (Cooldown/Varování)
        // Hráè má tento èas na to, aby utekl z kruhu
        yield return new WaitForSeconds(activationDelay);

        // 2. Kousnutí!
        // (Pokud máš v animaci speciální moment pro kousnutí, 
        //  mùžeš to naèasovat tak, aby activationDelay odpovídalo délce "pøípravné" fáze animace)

        CheckCapture();
    }

    void CheckCapture()
    {
        if (hasTriggered) return;

        // Kruhový test kolize pøesnì v místì pasti
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, LayerMask.GetMask("Player"));

        if (hit != null)
        {
            PlayerStats stats = hit.GetComponent<PlayerStats>();
            PlayerMovement move = hit.GetComponent<PlayerMovement>();

            // Udìlit damage
            if (stats != null)
            {
                stats.TakeDamage(damage);
            }

            // Aplikovat Stun
            if (move != null)
            {
                Debug.Log("HRÁÈ CHYCEN DO KOØENÙ!");
                move.ApplySlow(stunDuration, 0f);
            }

            hasTriggered = true;
        }
    }

    // Poznámka: OnTriggerEnter2D jsme odstranili, protože chceme, 
    // aby past "klapla" v jeden konkrétní moment, ne aby fungovala jako nášlapná mina.
    // Pokud v ní hráè v èase 'activationDelay' nestojí, má štìstí.
}