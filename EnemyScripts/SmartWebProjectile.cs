using UnityEngine;

public class SmartWebProjectile : MonoBehaviour
{
    public float speed = 8f;
    public int damage = 15;
    public int maxBounces = 1; // Kolikrát se mùže odrazit

    private Rigidbody2D rb;
    private int bounces = 0;
    private Transform player;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        Destroy(gameObject, 5f); // Pojistka
    }

    public void Launch(Vector2 direction)
    {
        // Nutné volat po instanciaci
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction.normalized * speed;

        // Otoèení spritu ve smìru letu
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Zásah hráèe
            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            // Zásah zdi
            if (bounces < maxBounces && player != null)
            {
                bounces++;
                // TRIK: Odraz se pøímo na hráèe
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