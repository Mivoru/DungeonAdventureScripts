using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage; // Nastavuje se automaticky při výstřelu
    public float lifeTime = 5f;

    [Header("Layers Detection")]
    public LayerMask enemyLayers;    // Koho má zranit
    public LayerMask obstacleLayers; // O co se má rozbít

    private Rigidbody2D rb;
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime); // Pojistka
    }

    void Update()
    {
        if (hasHit) return;

        // Otočení šípu ve směru letu (Unity 6 používá linearVelocity)
        if (rb.linearVelocity.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Pokud už jsme něco trefili, ignorujeme další kolize
        if (hasHit) return;

        int hitLayer = 1 << collision.gameObject.layer;

        // Debug výpis (můžeš pak smazat)
         Debug.Log($"Šíp trefil: {collision.name} na vrstvě {LayerMask.LayerToName(collision.gameObject.layer)}");

        // 1. NÁRAZ DO CÍLE (Podle nastavené masky)
        if ((enemyLayers.value & hitLayer) > 0)
        {
            // Hledáme rodičovský skript CharacterStats (funguje pro Player i Enemy)
            CharacterStats targetStats = collision.GetComponent<CharacterStats>();

            if (targetStats != null)
            {
                targetStats.TakeDamage(damage);
                hasHit = true;
                Destroy(gameObject); // Zničit šíp
            }
        }

        // 2. NÁRAZ DO ZDI
        else if ((obstacleLayers.value & hitLayer) > 0)
        {
            hasHit = true;

            // Zastavení šípu
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Zaseknutí na místě

            // Vypnutí kolizí, aby do šípu nešlo narážet
            Collider2D arrowCollider = GetComponent<Collider2D>();
            if (arrowCollider != null) arrowCollider.enabled = false;

            // Přilepení ke zdi
            transform.SetParent(collision.transform);

            Destroy(gameObject, 2f); // Zmizí po chvíli
        }
    }
}