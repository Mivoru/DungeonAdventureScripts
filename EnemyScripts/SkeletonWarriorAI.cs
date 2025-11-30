using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStats))]
public class SkeletonWarriorAI : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float patrolRadius = 5f;
    public float patrolWaitTime = 2f;

    [Header("Combat Distances")]
    public float aggroRange = 8f;       // Kdy si hráèe všimne
    public float protectRangeMax = 8f;  // Od této dálky se kryje
    public float protectRangeMin = 3.5f; // Když je blíž, radìji útoèí než kryje
    public float runAttackRange = 4.5f; // Kdy spustí Run Attack (musí být > meleeRange)
    public float meleeRange = 1.8f;     // Kdy stojí a bojuje na blízko

    [Header("Damage Settings")]
    public int lightDamage = 10;
    public int heavyDamage = 20;
    public int runAttackDamage = 25;
    public float attackCooldown = 2f;

    [Header("Layers")]
    public LayerMask projectileLayer; // Vrstva "PlayerProjectile" (pro blokování)
    public LayerMask playerLayer;     // Vrstva "Player" (pro útok)

    // STAVY
    private enum State { Patrol, Chase, Combat, Protect, RunAttack }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private EnemyStats stats;

    private float nextAttackTime;
    private float patrolTimer;
    private bool isActionInProgress = false; // Blokuje pohyb pøi útoku/bloku
    private Vector3 startPosition;
    private Vector3 baseScale; // Pro správné otáèení

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        startPosition = transform.position;
        baseScale = transform.localScale;

        SetPatrolPoint();
    }

    void Update()
    {
        if (player == null) return;
        if (isActionInProgress) return; // Pokud bojuje, nehýbe se logikou

        float dist = Vector2.Distance(transform.position, player.position);

        // Otáèení (vždy na hráèe, kromì hlídky)
        if (currentState != State.Patrol) RotateTowards(player.position);
        else if (agent.velocity.sqrMagnitude > 0.1f) RotateTowards(agent.steeringTarget);

        // Rozhodování
        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic(dist);
                break;
            case State.Chase:
                ChaseLogic(dist);
                break;
            case State.Combat:
                CombatLogic(dist);
                break;
        }

        UpdateAnimation();
    }

    // --- 1. HLÍDKOVÁNÍ ---
    void PatrolLogic(float dist)
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

        if (dist < aggroRange) currentState = State.Chase;
    }

    // --- 2. PRONÁSLEDOVÁNÍ ---
    void ChaseLogic(float dist)
    {
        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // A) KONTROLA ŠÍPÙ (PROTECT) - Má nejvyšší prioritu pøi bìhu
        if (dist < protectRangeMax && dist > protectRangeMin)
        {
            if (CheckForProjectiles())
            {
                StartCoroutine(PerformBlock());
                return;
            }
        }

        // B) KONTROLA RUN ATTACK (Výpad)
        if (dist <= runAttackRange && dist > meleeRange)
        {
            // 30% šance každým framem je moc, dáme menší, nebo èasovaè.
            // Tady dáme jednoduchý Random check, ale jen obèas
            if (UnityEngine.Random.value > 0.98f) // 2% šance každý frame (aby to neudìlal hned)
            {
                StartCoroutine(PerformRunAttack());
                return;
            }
        }

        // C) PØECHOD DO BOJE
        if (dist <= meleeRange)
        {
            currentState = State.Combat;
        }
    }

    // --- 3. BOJ NA BLÍZKO ---
    void CombatLogic(float dist)
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        // Pokud hráè utekl, zaèni ho honit
        if (dist > meleeRange * 1.5f)
        {
            currentState = State.Chase;
            return;
        }

        if (Time.time >= nextAttackTime)
        {
            // 30% Tìžký, 70% Lehký
            if (UnityEngine.Random.value > 0.7f)
                StartCoroutine(AttackRoutine(2, heavyDamage)); // 2 = Heavy
            else
                StartCoroutine(AttackRoutine(1, lightDamage)); // 1 = Light

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // --- AKCE: BLOKOVÁNÍ ---
    bool CheckForProjectiles()
    {
        // Hledá šípy (PlayerProjectile) v okruhu 3.5m
        return Physics2D.OverlapCircle(transform.position, 3.5f, projectileLayer) != null;
    }

    IEnumerator PerformBlock()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        agent.velocity = Vector2.zero;
        anim.SetFloat("Speed", 0);

        Debug.Log(" Warrior: Kryju se!");
        anim.SetBool("IsBlocking", true); // Spustí animaci Protect

        yield return new WaitForSeconds(1.5f); // Drží štít

        anim.SetBool("IsBlocking", false);
        isActionInProgress = false;
        currentState = State.Chase;
    }

    // --- AKCE: VÝPAD (Run Attack) ---
    IEnumerator PerformRunAttack()
    {
        isActionInProgress = true;
        Debug.Log(" Warrior: Run Attack!");

        // Pokud nemáš animaci RunAttack, použij Heavy (2) nebo Light (1)
        anim.SetInteger("AttackType", 3); // Pøedpokládáme 3 = RunAttack v Animátoru

        Vector3 dashDir = (player.position - transform.position).normalized;
        Vector3 dashTarget = transform.position + dashDir * 3f;

        agent.speed = runSpeed * 2.5f; // Zrychlení
        agent.SetDestination(dashTarget);

        yield return new WaitForSeconds(0.4f); // Èas do zásahu
        DealDamage(runAttackDamage, 2.5f); // Vìtší dosah
        yield return new WaitForSeconds(0.4f); // Dojezd

        anim.SetInteger("AttackType", 0);
        agent.speed = runSpeed;
        isActionInProgress = false;
        currentState = State.Combat;
    }

    // --- AKCE: KLASICKÝ ÚTOK ---
    IEnumerator AttackRoutine(int attackTypeID, int dmg)
    {
        isActionInProgress = true;
        agent.isStopped = true;

        // Natoèení na hráèe pøed úderem
        RotateTowards(player.position);

        anim.SetInteger("AttackType", attackTypeID);

        // Èasování zásahu (laï podle animace)
        yield return new WaitForSeconds(0.4f);

        DealDamage(dmg, meleeRange + 0.5f);

        yield return new WaitForSeconds(0.4f); // Konec animace

        anim.SetInteger("AttackType", 0);
        isActionInProgress = false;
    }

    // --- ÚTOK S OFFSETEM (Aby trefil i když stojíš "v nìm") ---
    void DealDamage(int dmg, float range)
    {
        // Zjistíme smìr pohledu (-1 nebo 1)
        float facingDir = Mathf.Sign(transform.localScale.x);

        // Kruh posuneme dopøedu o 1 metr
        Vector3 attackCenter = transform.position + new Vector3(facingDir * 1.0f, 0, 0);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, range, playerLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerStats>()?.TakeDamage(dmg);
        }
    }

    // --- POMOCNÉ ---
    public void TriggerAggro()
    {
        if (currentState == State.Patrol) currentState = State.Chase;
    }

    void SetPatrolPoint()
    {
        Vector3 p = startPosition + (Vector3)UnityEngine.Random.insideUnitSphere * patrolRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(p, out hit, patrolRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    void RotateTowards(Vector3 target)
    {
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
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, protectRangeMax);

        // Vizualizace útoèného kruhu (pro ladìní)
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector3 attackCenter = transform.position + new Vector3(facingDir * 1.0f, 0, 0);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCenter, meleeRange);
    }
}