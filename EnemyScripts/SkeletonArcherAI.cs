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
    public float meleeRange = 1.5f;
    public float fleeDistance = 3f;

    [Header("Cooldowns")]
    public float shootCooldown = 2f;
    public float meleeCooldown = 1.5f;
    public float evadeCooldown = 3f;

    [Header("Weapon (Bow)")]
    public GameObject arrowPrefab;
    public Transform firePoint;
    public float arrowSpeed = 15f;
    public int bowDamage = 10;

    [Header("Weapon (Dagger)")]
    public int daggerDamage = 5;

    [Header("Layers")]
    public LayerMask projectileLayer;
    public LayerMask playerLayer;

    // --- STAVY ---
    private enum State { Patrol, Chase, Shoot, Melee, Evade, Search, Flee }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private Vector3 startPosition;
    private Vector3 lastKnownPosition;

    private float nextShootTime;
    private float nextMeleeTime;
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
            case State.Melee:
                MeleeLogic(distToPlayer); // Opraveno: jen jeden argument
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

            if (dist <= meleeRange) currentState = State.Melee;
            else if (dist < fleeDistance) currentState = State.Flee;
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
        if (dist <= meleeRange) { currentState = State.Melee; agent.isStopped = false; return; }
        if (dist < fleeDistance) { currentState = State.Flee; agent.isStopped = false; return; }

        if (Time.time >= nextShootTime)
        {
            StartCoroutine(ShootRoutine());
            nextShootTime = Time.time + shootCooldown;
        }
    }

    void MeleeLogic(float dist)
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        if (dist > meleeRange * 1.5f) { currentState = State.Chase; agent.isStopped = false; return; }

        if (Time.time >= nextMeleeTime)
        {
            StartCoroutine(MeleeRoutine());
            nextMeleeTime = Time.time + meleeCooldown;
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

    IEnumerator MeleeRoutine()
    {
        isActionInProgress = true;
        anim.SetTrigger("Melee");
        yield return new WaitForSeconds(0.3f); // Časování zásahu

        // Útok dýkou (kruh) - s posunem vpřed
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector3 attackPos = transform.position + new Vector3(facingDir * 0.5f, 0, 0);

        Collider2D hit = Physics2D.OverlapCircle(attackPos, meleeRange, playerLayer);
        if (hit != null) hit.GetComponent<PlayerStats>()?.TakeDamage(daggerDamage);

        yield return new WaitForSeconds(0.5f); // Dojezd animace
        isActionInProgress = false;
    }

    IEnumerator PerformEvasion()
    {
        isActionInProgress = true;
        isEvading = true; // Imunita

        anim.SetTrigger("Evade");

        // Náhodný směr úskoku
        Vector3 evadeDir = transform.right * (UnityEngine.Random.value > 0.5f ? 1 : -1);
        if (Physics2D.Raycast(transform.position, evadeDir, 2f, obstacleLayer)) evadeDir = -transform.up;

        Vector3 targetPos = transform.position + evadeDir * 3f;

        agent.speed = runSpeed * 3f; // Turbo
        agent.SetDestination(targetPos);

        yield return new WaitForSeconds(0.5f);

        agent.speed = runSpeed;
        nextEvadeTime = Time.time + evadeCooldown;

        isActionInProgress = false;
        isEvading = false; // Konec imunity
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