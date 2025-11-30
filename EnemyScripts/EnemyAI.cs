using UnityEngine;
using UnityEngine.AI;

// Tento skript vyžaduje, aby na objektu byl NavMeshAgent a EnemyStats
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class EnemyAI : MonoBehaviour
{
    [Header("AI Type")]
    public bool isRanged = false;       // ZAŠKRTNI PRO LUÈIŠTNÍKA (Archer)

    [Header("Aggro Settings")]
    public float aggroRange = 8f;       // Kdy si hráèe všimne sám (zrak)

    [Header("Movement Settings")]
    public float stopDistance = 1.5f;   // Kdy zastaví u hráèe (pro útok)
    public float fleeDistance = 4.0f;   // Kdy zaène utíkat (jen pro luèištníka)

    // Veøejná vlastnost: Útoèné skripty (Shooter/Melee) budou èíst toto
    public bool IsInAttackRange { get; private set; }

    private NavMeshAgent agent;
    private Transform player;
    private EnemyStats stats;
    private bool hasAggro = false;      // Ví o nás?

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();

        // DÙLEŽITÉ PRO 2D: Zákaz rotace agenta (dìláme to ruènì)
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // Nastavení rychlosti podle statistik
        if (stats != null)
        {
            agent.speed = stats.movementSpeed;
        }

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Bezpeèný start na NavMeshi (aby nepropadl)
        StartCoroutine(InitialWarp());
    }

    // Volá se z EnemyStats, když dostane damage
    public void TriggerAggro()
    {
        hasAggro = true;
    }

    void Update()
    {
        if (player == null || !agent.isOnNavMesh) return;

        float distToPlayer = Vector2.Distance(transform.position, player.position);

        // 1. Získání Aggra (buï je blízko, nebo už byl vyprovokován)
        if (!hasAggro && distToPlayer <= aggroRange)
        {
            hasAggro = true;
        }

        // Pokud nemá aggro, stojí a nic nedìlá
        if (!hasAggro) return;

        // 2. Aktualizace rychlosti (kdyby se zmìnila levelem)
        if (stats != null) agent.speed = stats.movementSpeed;

        // 3. Rotace (aby se díval na hráèe)
        RotateTowardsPlayer();

        // 4. Rozhodování pohybu (Støelec vs Bojovník)
        if (isRanged)
        {
            HandleRangedMovement(distToPlayer);
        }
        else
        {
            HandleMeleeMovement(distToPlayer);
        }
    }

    void HandleMeleeMovement(float dist)
    {
        // Soldier: Bìží k hráèi, dokud není dost blízko na útok
        if (dist > stopDistance)
        {
            agent.SetDestination(player.position);
            IsInAttackRange = false;
            if (agent.isStopped) agent.isStopped = false;
        }
        else
        {
            // Je u hráèe -> Zastavit a útoèit
            agent.isStopped = true;
            agent.velocity = Vector2.zero;
            IsInAttackRange = true;
        }
    }

    void HandleRangedMovement(float dist)
    {
        // Archer: Kiting logika
        if (dist < fleeDistance)
        {
            // Hráè je MOC BLÍZKO -> UTÍKEJ!
            Vector3 dirToPlayer = transform.position - player.position;

            // Najdeme bod smìrem OD hráèe
            Vector3 fleePos = transform.position + dirToPlayer.normalized * 3f;

            agent.SetDestination(fleePos);
            agent.isStopped = false;
            IsInAttackRange = false; // Když utíká, nestøílí
        }
        else if (dist > stopDistance)
        {
            // Hráè je MOC DALEKO -> JDI BLÍŽ
            agent.SetDestination(player.position);
            agent.isStopped = false;
            IsInAttackRange = false;
        }
        else
        {
            // Hráè je TAK AKORÁT -> STÙJ A STØÍLEJ
            agent.isStopped = true;
            agent.velocity = Vector2.zero;
            IsInAttackRange = true;
        }
    }

    void RotateTowardsPlayer()
    {
        if (player == null) return;
        Vector2 dir = (player.position - transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        // -90 je korekce, pokud tvùj sprite kouká nahoru. Pokud doprava, dej 0.
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    System.Collections.IEnumerator InitialWarp()
    {
        yield return new WaitForSeconds(0.1f);
        // Pokus o opravu pozice na NavMeshi pøi startu
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 3.0f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    // Vizualizace v editoru
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        if (isRanged)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, fleeDistance);
        }
    }
}