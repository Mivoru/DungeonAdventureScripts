using UnityEngine;

public class SlimeAttack : MonoBehaviour
{
    [Header("Area Damage Settings")]
    public float impactRadius = 1.5f; // Jak velký je "výbuch" pøi dopadu
    public LayerMask targetLayer;     // Koho to zraòuje (Player)

    [Header("Slow Effect")]
    public float slowDuration = 2.0f; // Jak dlouho bude pomalý
    [Range(0.1f, 1f)]
    public float slowPercentage = 0.5f; // Na kolik % klesne rychlost (0.5 = polovina)

    private EnemyStats myStats;
    private Rigidbody2D rb;

    void Start()
    {
        myStats = GetComponent<EnemyStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Slime útoèí, když do nìèeho narazí (po skoku)
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Kontrola, aby útoèil jen pøi dopadu na zem nebo hráèe
        // (Mùžeš sem pøidat podmínku na rychlost, pokud chceš)
        PerformAreaAttack();
    }

    void PerformAreaAttack()
    {
        // 1. Najdeme všechny cíle v kruhu (AoE)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, impactRadius, targetLayer);

        foreach (Collider2D hit in hits)
        {
            // Zkontrolujeme, jestli je to hráè
            if (hit.CompareTag("Player"))
            {
                PlayerStats playerStats = hit.GetComponent<PlayerStats>();

                if (playerStats != null)
                {
                    // A. Zpùsobit poškození (bereme ze stats)
                    
                    if (myStats != null)
                    {
                        playerStats.TakeDamage(myStats.baseDamage);
                    }

                    // B. Zpùsobit zpomalení (OPRAVENO)
                    // Voláme to pøes PlayerStats, ne pøes PlayerMovement
                    // Pozor na poøadí argumentù: (síla, èas)
                    playerStats.ApplySlowness(slowPercentage, slowDuration);
                    
                }
            }
        }
    }

    // Vizualizace v editoru
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, impactRadius);
    }
}