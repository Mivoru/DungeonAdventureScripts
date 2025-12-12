using UnityEngine;

public class EntIvyProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 2f;
    public int damage = 30;

    [HideInInspector] public Vector2 direction;

    private Rigidbody2D rb;
    private bool hasHit = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifeTime);

        // Natoèení ve smìru letu
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    void FixedUpdate()
    {
        if (!hasHit)
        {
            rb.linearVelocity = direction * speed; // (Unity 6) - v Unity 2022 použij rb.velocity
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerStats>()?.TakeDamage(damage);
            hasHit = true;
            Destroy(gameObject); // Zásah -> Zmizet
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Destroy(gameObject); // Náraz do zdi -> Zmizet
        }
    }
}