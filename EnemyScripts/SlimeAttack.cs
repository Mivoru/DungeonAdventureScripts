using UnityEngine;

public class SlimeAttack : MonoBehaviour
{
    [Header("Area Damage Settings")]
    public float impactRadius = 1.5f; // Jak velkı je "vıbuch" pøi dopadu
    public LayerMask targetLayer;     // Koho to zraòuje (Player)

    [Header("Slow Effect")]
    public float slowDuration = 2.0f; // Jak dlouho bude pomalı
    [Range(0.1f, 1f)]
    public float slowPercentage = 0.5f; // Na kolik % klesne rychlost (0.5 = polovina)

    private EnemyStats myStats;
    private Rigidbody2D rb;

    void Start()
    {
        myStats = GetComponent<EnemyStats>();
        rb = GetComponent<Rigidbody2D>();
    }

    // Slime útoèí, kdy do nìèeho narazí (po skoku)
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Zkontrolujeme, jestli jsme narazili dostateènou rychlostí (aby to nebyl jen uk)
        // Nebo prostì pøi kadém dopadu na hráèe/zem

        // Pokud narazíme pøímo do hráèe NEBO do zdi/zemì blízko hráèe
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
                // A. Zpùsobit poškození (bereme ze stats)
                PlayerStats playerStats = hit.GetComponent<PlayerStats>();
                if (playerStats != null && myStats != null)
                {
                    playerStats.TakeDamage(myStats.baseDamage);
                }

                // B. Zpùsobit zpomalení
                PlayerMovement playerMove = hit.GetComponent<PlayerMovement>();
                if (playerMove != null)
                {
                    playerMove.ApplySlow(slowDuration, slowPercentage);
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