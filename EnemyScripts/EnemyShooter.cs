using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("Shooting Stats")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public float arrowSpeed = 15f;
    public int damage = 10;

    private float nextFireTime;
    private EnemyAI ai;

    void Start()
    {
        ai = GetComponent<EnemyAI>();
        if (ai == null) Debug.LogError("CHYBA: EnemyShooter nenašel skript EnemyAI!");
        if (arrowPrefab == null) Debug.LogError("CHYBA: Chybí Arrow Prefab v Inspectoru!");
        if (firePoint == null) Debug.LogError("CHYBA: Chybí Fire Point v Inspectoru!");
    }

    void Update()
    {
        // Pokud chybí AI, nic neděláme
        if (ai == null) return;

        // DEBUG: Zjistíme, jestli AI ví, že má útočit
        // (Toto odkomentuj jen pokud se nic neděje, jinak to zahltí konzoli)
        // Debug.Log($"IsInRange: {ai.IsInAttackRange} | TimeOK: {Time.time >= nextFireTime}");

        if (ai.IsInAttackRange)
        {
            if (Time.time >= nextFireTime)
            {
                Debug.Log(" PODMÍNKY SPLNĚNY -> VOLÁM SHOOT()!");
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void Shoot()
    {
        if (arrowPrefab == null || firePoint == null) return;

        Debug.Log(" INSTANTIATE: Vytvářím šíp!");
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

        if (arrow == null)
        {
            Debug.LogError("CHYBA: Šíp se nepodařilo vytvořit!");
            return;
        }

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = firePoint.up * arrowSpeed;
        }

        ArrowProjectile proj = arrow.GetComponent<ArrowProjectile>();
        if (proj != null)
        {
            proj.damage = damage;
            // Nastavení vrstev
            proj.enemyLayers = LayerMask.GetMask("Player");
            proj.obstacleLayers = LayerMask.GetMask("Obstacles");
        }
    }
}