using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class GiantProjectile : MonoBehaviour
{
    [Header("Settings")]
    public int damage = 20;
    public float speed = 10f;
    public float lifeTime = 10f;

    [Header("Rolling Behavior")]
    public bool isRolling = false; // Zaškrtni pro Rolling Rock
    public float rotateSpeed = 360f;

    private Rigidbody2D rb;
    private Animator anim; // Pro animaci rozpadu
    private bool hasHit = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        // Znièit po èase, pokud nic netrefí
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (hasHit) return;

        // Pokud je to valící se kámen, toèíme s ním vizuálnì
        if (isRolling)
        {
            transform.Rotate(0, 0, -rotateSpeed * Time.deltaTime);
        }
    }

    // Tuto metodu volá GiantAI pøi vytvoøení
    public void Initialize(Vector3 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = direction.normalized * speed;

        // Pokud letí vzduchem (není rolling), natoèíme ho èumákem dopøedu
        if (!isRolling)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return; // Už jsme nìco trefili
        if (collision.isTrigger) return; // Ignorujeme jiné triggery (napø. agro zóny)
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy")) return; // Ignorujeme bosse

        // Trefil Hráèe nebo Zeï
        if (collision.CompareTag("Player") || collision.gameObject.layer == LayerMask.NameToLayer("Obstacles"))
        {
            // Pokud je to hráè, dej damage
            if (collision.CompareTag("Player"))
            {
                collision.GetComponent<PlayerStats>()?.TakeDamage(damage);
            }

            StartCoroutine(BreakRoutine());
        }
    }

    IEnumerator BreakRoutine()
    {
        hasHit = true;
        rb.linearVelocity = Vector2.zero; // Zastavit pohyb

        // Vypneme collider, aby už nezraòoval
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Spustit animaci rozpadu (pokud existuje)
        if (anim != null)
        {
            anim.SetTrigger("Break"); // <--- Musíš mít tento Trigger v Animátoru kamene
            yield return new WaitForSeconds(0.5f); // Èas na pøehrání animace
        }
        else
        {
            // Pokud nemáš animaci, jen chvíli poèkej nebo hned zniè
            yield return null;
        }

        Destroy(gameObject);
    }
}