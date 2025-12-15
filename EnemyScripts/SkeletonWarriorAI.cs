using System;
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

    [Header("Search Logic")]
    public float searchDuration = 5f;
    public float searchRadius = 8f;

    [Header("Vision")]
    public float aggroRange = 8f;
    public LayerMask obstacleLayer; // Zdi

    [Header("Combat Distances")]
    public float protectRangeMax = 8f;
    public float protectRangeMin = 3.5f;
    public float meleeRange = 1.8f;

    [Header("Damage Settings")]
    public int lightDamage = 15;
    public int heavyDamage = 30;
    public float attackCooldown = 2f;

    [Header("Layers")]
    public LayerMask projectileLayer;
    public LayerMask playerLayer;

    private enum State { Patrol, Chase, Search, Combat, Protect }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private EnemyStats stats;

    private float nextAttackTime;
    private float patrolTimer;
    private float searchTimer;

    private Vector3 startPosition;
    private Vector3 baseScale;
    private Vector3 lastKnownPosition;
    private bool isActionInProgress = false;

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
        if (isActionInProgress) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool canSee = CheckLineOfSight();

        if (currentState != State.Patrol && currentState != State.Search) RotateTowards(player.position);
        else if (agent.velocity.sqrMagnitude > 0.1f) RotateTowards(agent.steeringTarget);

        switch (currentState)
        {
            case State.Patrol: PatrolLogic(dist, canSee); break;
            case State.Chase: ChaseLogic(dist, canSee); break;
            case State.Search: SearchLogic(canSee); break;
            case State.Combat: CombatLogic(dist); break;
        }

        UpdateAnimation();
    }

    // --- LOGIKA ---

    void PatrolLogic(float dist, bool canSee)
    {
        agent.speed = walkSpeed;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime) { SetPatrolPoint(); patrolTimer = 0; }
        }

        // Vidí hráèe -> Chase
        if (dist < aggroRange && canSee) currentState = State.Chase;
    }

    void ChaseLogic(float dist, bool canSee)
    {
        agent.speed = runSpeed;
        agent.isStopped = false;

        if (canSee)
        {
            lastKnownPosition = player.position;
            agent.SetDestination(player.position);

            // Blokování šípù
            if (dist < protectRangeMax && dist > protectRangeMin)
            {
                if (CheckForProjectiles()) { StartCoroutine(PerformBlock()); return; }
            }

            if (dist <= meleeRange) currentState = State.Combat;
        }
        else
        {
            currentState = State.Search;
        }
    }

    void SearchLogic(bool canSee)
    {
        if (canSee) { currentState = State.Chase; searchTimer = 0; return; }

        agent.speed = runSpeed;

        // Pokud je daleko od posledního místa, bìž tam
        if (Vector2.Distance(transform.position, lastKnownPosition) > 2f && searchTimer == 0)
        {
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            // Hledání v okruhu 8m
            searchTimer += Time.deltaTime;
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * searchRadius;
                Vector3 dest = lastKnownPosition + new Vector3(rnd.x, rnd.y, 0);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(dest, out hit, 2f, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            }

            if (searchTimer > searchDuration)
            {
                currentState = State.Patrol;
                SetPatrolPoint();
                searchTimer = 0;
            }
        }
    }

    void CombatLogic(float dist)
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        if (dist > meleeRange * 1.5f) { currentState = State.Chase; return; }

        if (Time.time >= nextAttackTime)
        {
            float r = UnityEngine.Random.value;
            if (r > 0.7f) StartCoroutine(AttackRoutine(2, heavyDamage, 0.8f));
            else StartCoroutine(AttackRoutine(1, lightDamage, 0.4f));

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // --- POMOCNÉ ---

    bool CheckLineOfSight()
    {
        Vector2 dir = player.position - transform.position;
        if (dir.magnitude > aggroRange) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, aggroRange, obstacleLayer);
        if (hit.collider != null && hit.transform != player) return false; // Trefilo zeï
        return true;
    }

    public void TriggerAggro() { lastKnownPosition = player.position; currentState = State.Chase; }

    bool CheckForProjectiles() { return Physics2D.OverlapCircle(transform.position, 3.5f, projectileLayer) != null; }

    IEnumerator PerformBlock()
    {
        isActionInProgress = true; agent.isStopped = true; anim.SetFloat("Speed", 0);
        anim.SetBool("IsBlocking", true); yield return new WaitForSeconds(1.5f); anim.SetBool("IsBlocking", false);
        isActionInProgress = false; currentState = State.Chase;
    }

    IEnumerator AttackRoutine(int typeID, int dmg, float delay)
    {
        isActionInProgress = true; agent.isStopped = true; RotateTowards(player.position);
        anim.SetInteger("AttackType", typeID); yield return new WaitForSeconds(delay);
        DealDamage(dmg, meleeRange + 0.5f); yield return new WaitForSeconds(0.5f);
        anim.SetInteger("AttackType", 0); isActionInProgress = false;
    }

    void DealDamage(int dmg, float range)
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector3 attackCenter = transform.position + new Vector3(facingDir * 0.8f, 0, 0);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, range, playerLayer);
        foreach (var hit in hits) hit.GetComponent<PlayerStats>()?.TakeDamage(dmg);
    }

    void SetPatrolPoint()
    {
        Vector3 p = startPosition + (Vector3)UnityEngine.Random.insideUnitSphere * patrolRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(p, out hit, patrolRadius, NavMesh.AllAreas)) agent.SetDestination(hit.position);
    }

    void RotateTowards(Vector3 target)
    {
        if (target.x < transform.position.x) transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void UpdateAnimation()
    {
        if (isActionInProgress) { anim.SetFloat("Speed", 0); return; }
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);
    }
}