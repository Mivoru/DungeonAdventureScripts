using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class SkeletonArcherAI : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;
    public float patrolRadius = 6f;
    public float patrolWaitTime = 2f;

    [Header("Vision & Aggro")]
    public float aggroRange = 10f;
    public LayerMask obstacleLayer;
    public float searchTime = 4f;

    [Header("Combat Settings")]
    public float idealRange = 6f;
    public float fleeDistance = 3f;

    [Header("Cooldowns")]
    public float shootCooldown = 2f;
    public float evadeCooldown = 3f;

    [Header("Weapon (Bow)")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowSpeed = 15f;
    public int bowDamage = 10;

    [Header("Layers")]
    public LayerMask projectileLayer;
    public LayerMask playerLayer;

    // --- STAVY ---
    private enum State { Patrol, Chase, Shoot, Evade, Search, Flee }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private Vector3 startPosition;
    private Vector3 lastKnownPosition;

    private float nextShootTime;
    private float nextEvadeTime;
    private float patrolTimer;
    private float searchTimer;

    private bool isActionInProgress = false;
    private Vector3 baseScale;

    // Proměnná pro imunitu (ptá se na ni EnemyStats)
    private bool isEvading = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        baseScale = transform.localScale;
        startPosition = transform.position;

        SetPatrolPoint();
    }

    void Update()
    {
        if (player == null) return;

        // Pokud probíhá animace (útok/úhyb), neměníme stav
        if (isActionInProgress) return;

        // 1. KONTROLA ÚHYBU (Priorita)
        if (Time.time >= nextEvadeTime && CheckForProjectiles())
        {
            StartCoroutine(PerformEvasion());
            return;
        }

        float distToPlayer = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = HasLineOfSight();

        // 2. ROZHODOVÁNÍ STAVŮ
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic(distToPlayer, canSeePlayer);
                break;
            case State.Chase:
                ChaseLogic(distToPlayer, canSeePlayer);
                break;
            case State.Shoot:
                ShootLogic(distToPlayer, canSeePlayer);
                break;
            case State.Flee:
                FleeLogic(distToPlayer);
                break;
            case State.Search:
                SearchLogic(distToPlayer, canSeePlayer);
                break;
        }

        // Otáčení
        if (currentState != State.Patrol && currentState != State.Search)
        {
            RotateTowards(player.position);
        }
        else if (agent.velocity.sqrMagnitude > 0.1f)
        {
            RotateTowards(transform.position + (Vector3)agent.velocity);
        }

        UpdateAnimation();
    }

    // --- LOGIKA STAVŮ ---

    void PatrolLogic(float dist, bool see)
    {
        agent.speed = walkSpeed;

        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                SetPatrolPoint();
                patrolTimer = 0;
            }
        }

        if (dist < aggroRange && see) currentState = State.Chase;
    }

    void ChaseLogic(float dist, bool see)
    {
        agent.speed = runSpeed;

        if (see)
        {
            lastKnownPosition = player.position;

            if (dist < fleeDistance) currentState = State.Flee;
            else if (dist <= idealRange) currentState = State.Shoot;
            else agent.SetDestination(player.position);
        }
        else
        {
            currentState = State.Search;
        }
    }

    void ShootLogic(float dist, bool see)
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        if (!see || dist > idealRange * 1.2f) { currentState = State.Chase; agent.isStopped = false; return; }
        if (dist < fleeDistance) { currentState = State.Flee; agent.isStopped = false; return; }

        if (Time.time >= nextShootTime)
        {
            StartCoroutine(ShootRoutine());
            nextShootTime = Time.time + shootCooldown;
        }
    }

    void FleeLogic(float dist)
    {
        agent.speed = runSpeed;

        if (dist > fleeDistance * 1.2f)
        {
            currentState = State.Chase; // Opraveno: Návrat do Chase
            return;
        }

        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        Vector3 targetFleePos = transform.position + dirFromPlayer * 4f;

        NavMeshHit hit;
        // Smart Flee: Zkoušíme dozadu, pak doprava, pak doleva
        if (!NavMesh.SamplePosition(targetFleePos, out hit, 2.0f, NavMesh.AllAreas))
        {
            Vector3 rightDir = Quaternion.Euler(0, 0, -90) * dirFromPlayer;
            Vector3 leftDir = Quaternion.Euler(0, 0, 90) * dirFromPlayer;

            if (NavMesh.SamplePosition(transform.position + rightDir * 3f, out hit, 1.0f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
            else if (NavMesh.SamplePosition(transform.position + leftDir * 3f, out hit, 1.0f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(hit.position);
        }
    }

    void SearchLogic(float dist, bool see)
    {
        if (see) { currentState = State.Chase; return; }

        agent.speed = runSpeed;
        agent.SetDestination(lastKnownPosition);

        if (agent.remainingDistance < 1f)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer > searchTime)
            {
                currentState = State.Patrol;
                searchTimer = 0;
            }
        }
    }

    // --- AKCE (COROUTINES) ---

    IEnumerator ShootRoutine()
    {
        isActionInProgress = true;
        anim.SetTrigger("Shoot");

        // Čekáme na Animation Event (ten zavolá ShootProjectile)
        // Dáme tomu čas (délka animace)
        yield return new WaitForSeconds(1.0f);

        isActionInProgress = false;
    }

    IEnumerator PerformEvasion()
    {
        isActionInProgress = true;
        isEvading = true; // Imunita zapnuta

        anim.SetTrigger("Evade");

        // 1. ZJISTÍME SMĚR ÚSKOKU (Podle trajektorie šípu)
        Vector3 dodgeDirection;

        // Najdeme nejbližší šíp
        Collider2D arrow = Physics2D.OverlapCircle(transform.position, 6f, projectileLayer);

        if (arrow != null && arrow.attachedRigidbody != null)
        {
            // Získáme směr letu šípu
            Vector2 arrowVelocity = arrow.attachedRigidbody.linearVelocity; // (V Unity 6)
            // Pokud máš starší Unity, použij: arrow.attachedRigidbody.velocity;

            if (arrowVelocity != Vector2.zero)
            {
                // Vypočítáme kolmici (Normalu) k letu šípu = Úskok do boku
                // Kolmice k (x, y) je (-y, x)
                dodgeDirection = new Vector3(-arrowVelocity.y, arrowVelocity.x, 0).normalized;
            }
            else
            {
                // Záloha: Kolmo k hráči
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                dodgeDirection = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0);
            }
        }
        else
        {
            // Záloha: Náhodně do boku
            dodgeDirection = transform.right;
        }

        // Náhodně doleva nebo doprava (50/50)
        if (UnityEngine.Random.value > 0.5f) dodgeDirection = -dodgeDirection;

        // 2. MANUÁLNÍ POHYB (DASH)
        // Místo SetDestination použijeme Move(), který neřeší pathfinding, jen kolize.

        agent.isStopped = true; // Vypneme automatický pohyb
        agent.ResetPath();

        float dashDuration = 0.4f; // Jak dlouho skáče
        float dashSpeed = 12f;     // Jak rychle skáče (hodně!)
        float timer = 0f;

        while (timer < dashDuration)
        {
            // Pohneme agentem manuálně o kousek v každém snímku
            // agent.Move respektuje zdi (neprojde skrz), ale nesnaží se hledat cestu
            agent.Move(dodgeDirection * dashSpeed * Time.deltaTime);

            timer += Time.deltaTime;
            yield return null; // Počkáme na další snímek
        }

        // 3. KONEC
        agent.isStopped = false; // Zapneme zpět mozek
        nextEvadeTime = Time.time + evadeCooldown;

        isActionInProgress = false;
        isEvading = false; // Vypneme imunitu
    }
    // --- PUBLIC METODY (ANIMATION EVENTS & STATS) ---

    // Tuto metodu volá Animation Event v animaci Shot1_Archer
    public void ShootProjectile()
    {
        if (arrowPrefab == null || firePoint == null || player == null) return;

        // Střílíme směrem k hráči
        Vector2 direction = (player.position - firePoint.position).normalized;

        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = direction * arrowSpeed;

        ArrowProjectile proj = arrow.GetComponent<ArrowProjectile>();
        if (proj != null) proj.damage = bowDamage;
    }

    // Volá EnemyStats při zásahu
    public void TriggerAggro()
    {
        lastKnownPosition = player.position;
        if (!isActionInProgress) currentState = State.Chase;
    }

    // Volá EnemyStats pro kontrolu imunity
    public bool IsEvading()
    {
        return isEvading;
    }

    // --- POMOCNÉ FUNKCE ---

    bool CheckForProjectiles()
    {
        return Physics2D.OverlapCircle(transform.position, 4f, projectileLayer) != null;
    }

    bool HasLineOfSight()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.position - transform.position, aggroRange, obstacleLayer);
        return hit.collider == null;
    }

    void SetPatrolPoint()
    {
        Vector3 p = startPosition + (Vector3)UnityEngine.Random.insideUnitSphere * patrolRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(p, out hit, patrolRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        RotateTowards(agent.destination);
    }

    void RotateTowards(Vector3 target)
    {
        // Otáčení pomocí Scale X (Flip)
        // Používáme baseScale, abychom zachovali velikost z Inspectoru
        if (target.x < transform.position.x)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void UpdateAnimation()
    {
        if (isActionInProgress) { anim.SetFloat("Speed", 0); return; }
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, idealRange);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, fleeDistance);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, 4f);
    }
}