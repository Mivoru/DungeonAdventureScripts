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

    [Header("Combat Distances")]
    public float aggroRange = 8f;
    public float protectRangeMax = 8f;
    public float protectRangeMin = 3.5f;
    public float meleeRange = 1.8f;

    [Header("Damage Settings")]
    public int lightDamage = 15;      // Menší damage
    public int heavyDamage = 30;      // Vìtší damage
    public float attackCooldown = 2f;

    [Header("Layers")]
    public LayerMask projectileLayer;
    public LayerMask playerLayer;

    private enum State { Patrol, Chase, Combat, Protect }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private EnemyStats stats;

    private float nextAttackTime;
    private float patrolTimer;
    private bool isActionInProgress = false;
    private Vector3 startPosition;
    private Vector3 baseScale;

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

        if (currentState != State.Patrol) RotateTowards(player.position);
        else if (agent.velocity.sqrMagnitude > 0.1f) RotateTowards(agent.steeringTarget);

        switch (currentState)
        {
            case State.Patrol: PatrolLogic(dist); break;
            case State.Chase: ChaseLogic(dist); break;
            case State.Combat: CombatLogic(dist); break;
        }

        UpdateAnimation();
    }

    // --- POHYB A LOGIKA ---

    void PatrolLogic(float dist)
    {
        agent.speed = walkSpeed;
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime) { SetPatrolPoint(); patrolTimer = 0; }
        }
        if (dist < aggroRange) currentState = State.Chase;
    }

    void ChaseLogic(float dist)
    {
        agent.speed = runSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // Krytí pøed šípy
        if (dist < protectRangeMax && dist > protectRangeMin)
        {
            if (CheckForProjectiles()) { StartCoroutine(PerformBlock()); return; }
        }

        // Boj
        if (dist <= meleeRange) currentState = State.Combat;
    }

    void CombatLogic(float dist)
    {
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        if (dist > meleeRange * 1.5f) { currentState = State.Chase; return; }

        if (Time.time >= nextAttackTime)
        {
            // ROZHODOVÁNÍ: LIGHT nebo HEAVY?
            float randomVal = UnityEngine.Random.value; // Èíslo 0.0 až 1.0

            if (randomVal > 0.7f) // 30% šance na Tìžký útok
            {
                // Typ 2 = Heavy, Damage, Zpoždìní zásahu 0.8s
                StartCoroutine(AttackRoutine(2, heavyDamage, 0.8f));
            }
            else // 70% šance na Lehký útok
            {
                // Typ 1 = Light, Damage, Zpoždìní zásahu 0.4s
                StartCoroutine(AttackRoutine(1, lightDamage, 0.4f));
            }

            nextAttackTime = Time.time + attackCooldown;
        }
    }

    // --- AKCE ---

    bool CheckForProjectiles()
    {
        return Physics2D.OverlapCircle(transform.position, 3.5f, projectileLayer) != null;
    }

    IEnumerator PerformBlock()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetFloat("Speed", 0);

        anim.SetBool("IsBlocking", true);
        yield return new WaitForSeconds(1.5f);
        anim.SetBool("IsBlocking", false);

        isActionInProgress = false;
        currentState = State.Chase;
    }

    // UNIVERZÁLNÍ ÚTOÈNÁ RUTINA
    // typeID: 1=Light, 2=Heavy
    // dmg: kolik to ubere
    // delay: kdy pøesnì ve animaci má dojít k zásahu (Heavy je pomalejší)
    IEnumerator AttackRoutine(int typeID, int dmg, float delay)
    {
        isActionInProgress = true;
        agent.isStopped = true;
        RotateTowards(player.position);

        // Spustíme animaci (1 nebo 2)
        anim.SetInteger("AttackType", typeID);

        // Èekáme na moment úderu
        yield return new WaitForSeconds(delay);

        // Kontrola zásahu
        DealDamage(dmg, meleeRange + 0.5f);

        // Èekáme na dokonèení animace (zbytek èasu)
        yield return new WaitForSeconds(0.5f);

        // Reset
        anim.SetInteger("AttackType", 0);
        isActionInProgress = false;
    }

    void DealDamage(int dmg, float range)
    {
        float facingDir = Mathf.Sign(transform.localScale.x);
        Vector3 attackCenter = transform.position + new Vector3(facingDir * 0.8f, 0, 0);

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, range, playerLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerStats>()?.TakeDamage(dmg);
        }
    }

    // --- POMOCNÉ ---

    public void TriggerAggro() { if (currentState == State.Patrol) currentState = State.Chase; }

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
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}