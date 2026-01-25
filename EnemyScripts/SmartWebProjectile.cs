using UnityEngine;

public class SmartWebProjectile : MonoBehaviour
{
    [Header("Pohyb")]
    public float speed = 8f;
    public int maxBounces = 1; // 1 = Jeden odraz, pak znièení

    [Header("Dopad (Impact)")]
    public int impactDamage = 15; // Okamžité poškození

    [Header("Efekty (PlayerStats)")]
    public bool applyPoison = true;
    public int poisonDamagePerTick = 5;
    public float poisonDuration = 3f;

    public bool applySlow = true;
    public float slowFactor = 0.5f; // Zpomalí na 50%
    public float slowDuration = 2f;

    // Interní promìnné
    private Rigidbody2D rb;
    private int bounces = 0;
    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Najdeme hráèe, abychom vìdìli, kam se odrážet
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        Destroy(gameObject, 5f); // Pojistka: znièit po 5 vteøinách, kdyby vyletìl z mapy
    }

    public void Launch(Vector2 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // Nastavení rychlosti
        // (Poznámka: v Unity 6 se používá linearVelocity, ve starších velocity)
        rb.linearVelocity = direction.normalized * speed;

        // Otoèení spritu ve smìru letu (aby šipka/pavuèina koukala dopøedu)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. ZÁSAH HRÁÈE
        if (other.CompareTag("Player"))
        {
            PlayerStats stats = other.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(impactDamage);
                if (applyPoison) stats.ApplyPoison(poisonDuration, poisonDamagePerTick);
                if (applySlow) stats.ApplySlowness(slowFactor, slowDuration);
            }
            Destroy(gameObject);
        }
        // 2. ZÁSAH ZDI (Oprava: Kontrolujeme JEN vrstvu "Obstacles")
        // Smazali jsme CompareTag("Wall"), takže už to nebude házet chybu.
        else if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            // Pokud ještì máme povolené odrazy a hráè žije
            if (bounces < maxBounces && player != null)
            {
                bounces++;
                Vector2 dirToPlayer = (player.position - transform.position).normalized;
                Launch(dirToPlayer);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}