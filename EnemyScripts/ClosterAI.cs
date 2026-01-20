using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class ClosterAI : MonoBehaviour
{
    [Header("Movement Stats")]
    public float patrolSpeed = 2f;
    public float chargeSpeed = 6f;
    public float patrolRadius = 5f;

    [Header("Vision & Radar")]
    public float detectionRange = 12f;
    public float arrowDetectionRange = 4f;
    public LayerMask obstacleMask;

    [Header("Combat Stats")]
    public float attackRange = 2.5f;
    public float attackCooldown = 1.5f;
    public int biteDamage = 15;

    [Header("Layers")]
    public LayerMask playerLayer; // TOTO JE DÙLEŽITÉ (stejnì jako u Skeletona)

    [Header("Jump & Dodge")]
    public float jumpRange = 5.5f;
    public float jumpCooldown = 4f;
    [Range(0, 100)] public int arrowJumpChance = 30;

    private Transform player;
    private NavMeshAgent agent;
    private Animator anim;
    private EnemyHealth health; // Pro vlastní životy
    // private EnemyStats stats; // Pokud bys chtìl používat staty jako Skeleton

    private bool isActing = false;
    private float nextJumpTime;
    private float nextAttackTime;
    private float idleTimer;
    private Vector3 lastKnownPosition;
    private ClosterAudio audioManager;
    
    void Start()
    {
        audioManager = GetComponent<ClosterAudio>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.stoppingDistance = 1.8f;
    }

    void Update()
    {
        if (player == null || health.currentHealth <= 0) return;
        if (isActing) return;

        // 1. Radar na šípy
        if (CheckForArrows()) return;

        // 2. Logika pohybu
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        bool seesPlayer = CheckLineOfSight(distToPlayer);

        if (seesPlayer) FaceTarget(player.position);
        else if (agent.velocity.sqrMagnitude > 0.1f) FaceTarget(transform.position + agent.velocity);

        if (seesPlayer)
        {
            lastKnownPosition = player.position;
            anim.SetBool("IsCharging", true);

            if (distToPlayer <= attackRange)
            {
                if (Time.time >= nextAttackTime)
                    StartCoroutine(PerformAttack());
                else
                    StopAgent();
            }
            else if (distToPlayer <= jumpRange && distToPlayer > attackRange && Time.time >= nextJumpTime)
            {
                if (UnityEngine.Random.Range(0, 100) < 60)
                    StartCoroutine(PerformJump());
                else
                    MoveToPlayer();
            }
            else
            {
                MoveToPlayer();
            }
        }
        else
        {
            HandleLostPlayer();
        }

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f && !agent.isStopped;
        anim.SetBool("IsMoving", isMoving);
        if (audioManager != null)
        {
            // Zjistíme, jestli charguje (podle animátoru nebo logiky)
            bool isCharging = anim.GetBool("IsCharging");
            audioManager.HandleMovementSound(isMoving, isCharging);
        }
    }

    // --- DAMAGE SYSTÉM (Pøevedeno ze SkeletonWarriorAI) ---

    void DealDamage(int dmg, float range)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range, playerLayer);

        foreach (var hit in hits)
        {
            // Hledáme PlayerStats (vèetnì rodièù)
            var stats = hit.GetComponentInParent<PlayerStats>();

            if (stats != null)
            {
                // 1. Udìlíme damage
                stats.TakeDamage(dmg);

                // 2. Aplikujeme jed
                stats.ApplyPoison(5f, 2);

                Debug.Log("<color=green>ZÁSAH! Damage + Jed aplikován pøes PlayerStats.</color>");
            }
            else
            {
                // Pokud to stále nenajde, zkusíme hledat v dìtech (pokud trefil hlavní objekt)
                stats = hit.GetComponentInChildren<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDamage(dmg);
                    stats.ApplyPoison(5f, 2);
                }
                else
                {
                    Debug.LogError($"Trefil jsem {hit.name}, ale PlayerStats tu fakt není!");
                }
            }
        }
    }

    IEnumerator PerformAttack()
    {
        isActing = true;
        StopAgent();
        anim.SetBool("IsMoving", false);

        FaceTarget(player.position);
        anim.SetTrigger("Attack");
        if (audioManager != null) audioManager.PlayAttack();
        yield return new WaitForSeconds(0.3f); // Èas než dopadnou zuby

        // Použití nové metody DealDamage
        DealDamage(biteDamage, attackRange);

        yield return new WaitForSeconds(0.5f);
        nextAttackTime = Time.time + attackCooldown;
        isActing = false;
    }

    IEnumerator PerformJump()
    {
        isActing = true;
        StopAgent();
        anim.SetBool("IsCharging", false);
        anim.SetTrigger("Jump");
        health.isInvincible = true;
        if (audioManager != null) audioManager.PlayJump();
        FaceTarget(player.position);

        yield return new WaitForSeconds(0.2f);

        Vector3 startPos = transform.position;
        Vector3 targetPos = player.position;
        float timer = 0f;
        float jumpDuration = 0.5f;

        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, timer / jumpDuration);
            FaceTarget(targetPos);
            yield return null;
        }

        health.isInvincible = false;
        nextJumpTime = Time.time + jumpCooldown;

        // Damage po dopadu
        DealDamage(biteDamage, attackRange);

        yield return new WaitForSeconds(0.2f);
        isActing = false;
    }

    // --- ZBYTEK ---

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90));
    }

    bool CheckForArrows()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, arrowDetectionRange);
        foreach (Collider2D hit in hits)
        {
            if (hit.GetComponent<ArrowProjectile>() != null)
            {
                if (UnityEngine.Random.Range(0, 100) < arrowJumpChance)
                    StartCoroutine(PerformJump());
                else
                    StartCoroutine(PerformDodge(hit.transform));
                return true;
            }
        }
        return false;
    }

    IEnumerator PerformDodge(Transform arrow)
    {
        isActing = true;
        Vector2 arrowDir = arrow.GetComponent<Rigidbody2D>().linearVelocity.normalized;
        Vector2 dodgeDir = Vector2.Perpendicular(arrowDir);
        Vector2 finalDir = (dodgeDir + (Vector2)(player.position - transform.position).normalized).normalized;

        agent.isStopped = true;
        agent.ResetPath();
        anim.SetBool("IsCharging", true);

        float timer = 0f;
        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            transform.position += (Vector3)finalDir * (chargeSpeed * 2f) * Time.deltaTime;
            FaceTarget(transform.position + (Vector3)finalDir);
            yield return null;
        }

        agent.isStopped = false;
        isActing = false;
    }

    void MoveToPlayer()
    {
        if (agent.isStopped) agent.isStopped = false;
        agent.speed = chargeSpeed;
        agent.SetDestination(player.position);
    }

    void StopAgent()
    {
        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    bool CheckLineOfSight(float dist)
    {
        if (dist > detectionRange) return false;
        Vector2 origin = transform.position + Vector3.up * 0.5f;
        Vector2 target = player.position + Vector3.up * 0.5f;
        Vector2 dir = (target - origin).normalized;
        RaycastHit2D hit = Physics2D.Raycast(origin, dir, dist, obstacleMask);
        return hit.collider == null;
    }

    void HandleLostPlayer()
    {
        if (lastKnownPosition != Vector3.zero && Vector3.Distance(transform.position, lastKnownPosition) > 1.5f)
        {
            MoveToPlayer();
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            anim.SetBool("IsCharging", false);
            agent.speed = patrolSpeed;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0)
                {
                    Vector3 randomPoint = UnityEngine.Random.insideUnitCircle * patrolRadius;
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(transform.position + randomPoint, out hit, 5f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                        idleTimer = UnityEngine.Random.Range(2f, 4f);
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, arrowDetectionRange);
    }
}