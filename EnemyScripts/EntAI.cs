using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EntAI : BaseEnemyAI
{
    [Header("Detection & Patrol")]
    public float patrolRadius = 5f;
    public float patrolWaitTime = 3f;
    public float aggroRange = 10f;
    public LayerMask obstacleLayer; // Vrstva zdí (aby nevidìl skrz)

    [Header("Search Settings")]
    public float searchDuration = 5f; // Jak dlouho hledá
    public float searchRadius = 8f;   // Kde všude hledá (8m okruh)

    [Header("Combat Settings")]
    public float meleeRange = 2.5f;
    public float rangeAttackDistance = 7f;

    [Header("Attacks Damage")]
    public int meleeDamage = 20;
    public int ivyDamage = 35;

    [Header("Cooldowns")]
    public float meleeCooldown = 2f;
    public float ivyCooldown = 5f;
    public float stunCooldown = 10f;

    [Header("Prefabs")]
    public GameObject ivyProjectilePrefab;
    public Transform ivySpawnPoint;
    public GameObject rootTrapPrefab;

    // --- STAVY ---
    private enum State { Patrol, Chase, Search, Combat }
    private State currentState = State.Patrol;

    private Vector3 lastKnownPosition;
    private float patrolTimer;
    private float searchTimer;

    // Èasovaèe útokù
    private float nextMeleeTime;
    private float nextIvyTime;
    private float nextStunTime;

    public override void Start()
    {
        base.Start(); // Volá start z BaseEnemyAI (kde se pravdìpodobnì nastavuje startPosition)

        // Pokud BaseEnemyAI nenastavuje startPosition, nastavíme ji zde. 
        // Pokud ji nastavuje, tento øádek je redundantní, ale neškodný.
        if (startPosition == Vector3.zero)
            startPosition = transform.position;

        nextStunTime = Time.time + 3f;
        SetPatrolPoint();
    }

    public override void Update()
    {
        base.Update(); // Base øeší animaci pohybu (Speed)

        if (player == null) return;
        if (isActionInProgress)
        {
            if (agent.enabled) agent.isStopped = true;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        bool canSee = CheckLineOfSight();

        switch (currentState)
        {
            case State.Patrol:
                PatrolLogic(dist, canSee);
                break;

            case State.Chase:
                ChaseLogic(dist, canSee);
                break;

            case State.Search:
                SearchLogic(canSee);
                break;

            case State.Combat:
                CombatLogic(dist, canSee);
                break;
        }
    }

    // --- LOGIKA STAVÙ ---

    void PatrolLogic(float dist, bool canSee)
    {
        agent.speed = 1.5f; // Pomalá chùze
        agent.isStopped = false;

        // Pokud došel do cíle patrolu
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            patrolTimer += Time.deltaTime;
            if (patrolTimer >= patrolWaitTime)
            {
                SetPatrolPoint();
                patrolTimer = 0;
            }
        }

        // AGGRO: Vidí hráèe A je blízko
        if (dist < aggroRange && canSee)
        {
            currentState = State.Chase;
        }
    }

    void ChaseLogic(float dist, bool canSee)
    {
        agent.speed = 3.5f; // Bìh
        agent.isStopped = false;

        if (canSee)
        {
            // Vidíme hráèe -> aktualizujeme pozici a jdeme po nìm
            lastKnownPosition = player.position;
            agent.SetDestination(player.position);

            // Pøechod do útoku
            if (dist <= meleeRange || (dist <= rangeAttackDistance && CanUseRangeAttack()))
            {
                currentState = State.Combat;
            }
        }
        else
        {
            // Nevidíme hráèe -> Jdeme hledat tam, kde byl naposledy
            currentState = State.Search;
        }
    }

    void SearchLogic(bool canSee)
    {
        // 1. Pokud ho zmerèíme bìhem hledání, okamžitì útok
        if (canSee)
        {
            currentState = State.Chase;
            searchTimer = 0;
            return;
        }

        agent.speed = 3.5f;

        // 2. Došli jsme už na místo posledního spatøení?
        float distToLastKnown = Vector2.Distance(transform.position, lastKnownPosition);

        if (distToLastKnown > 2.0f && searchTimer == 0)
        {
            // Ještì tam nejsme, bìžíme tam
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            // 3. Jsme v oblasti -> zaèínáme šmejdit okolo (Search Radius 8)
            searchTimer += Time.deltaTime;

            // Pokud zrovna nemá kam jít, dáme mu náhodný bod v okolí
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * searchRadius;
                Vector3 searchDest = lastKnownPosition + new Vector3(randomPoint.x, randomPoint.y, 0);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(searchDest, out hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            // 4. Vypršel èas?
            if (searchTimer > searchDuration)
            {
                currentState = State.Patrol;
                SetPatrolPoint();
                searchTimer = 0;
            }
        }
    }

    void CombatLogic(float dist, bool canSee)
    {
        agent.isStopped = true;

        // Pokud hráè utekl z boje nebo se schoval
        if (dist > rangeAttackDistance * 1.2f || !canSee)
        {
            currentState = State.Chase;
            return;
        }

        // Logika útokù (Priorita: Melee -> Stun -> Ivy)
        if (dist <= meleeRange && Time.time >= nextMeleeTime)
        {
            StartCoroutine(MeleeAttack());
        }
        else if (dist <= rangeAttackDistance)
        {
            if (Time.time >= nextStunTime && UnityEngine.Random.value > 0.6f)
            {
                StartCoroutine(StunAttack());
            }
            else if (Time.time >= nextIvyTime)
            {
                StartCoroutine(IvyRangeAttack());
            }
            else
            {
                // Všechno má cooldown, ale hráè je daleko -> popojdi k nìmu
                currentState = State.Chase;
            }
        }
    }

    // --- POMOCNÉ FUNKCE ---

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        Vector2 dir = player.position - transform.position;
        if (dir.magnitude > aggroRange) return false; // Moc daleko na vidìní

        // Raycast hledá zeï
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, aggroRange, obstacleLayer);

        // Pokud trefí Collider, a ten collider NENÍ hráè (tedy je to zeï), vrátí false
        // Poznámka: Ujisti se, že Player má na sobì taky Collider, jinak Raycast projde skrz nìj
        // Bezpeènìjší verze:
        if (hit.collider != null)
        {
            // Trefili jsme nìco. Pokud to není hráè, je to pøekážka.
            if (hit.transform != player) return false;
        }

        return true;
    }

    void SetPatrolPoint()
    {
        // Používáme zdìdìnou startPosition
        Vector3 randomPoint = startPosition + (Vector3)UnityEngine.Random.insideUnitSphere * patrolRadius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    bool CanUseRangeAttack()
    {
        return Time.time >= nextIvyTime || Time.time >= nextStunTime;
    }

    public void TriggerAggro()
    {
        // Voláno externì pøi zásahu
        lastKnownPosition = player.position;
        currentState = State.Chase;
    }

    // --- ÚTOKY (Coroutines) ---

    IEnumerator MeleeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("Attack_Type", 1);
        yield return new WaitForSeconds(0.5f);

        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeRange + 1f)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(0.5f);
        anim.SetInteger("Attack_Type", 0);
        nextMeleeTime = Time.time + meleeCooldown;
        isActionInProgress = false;
    }

    IEnumerator IvyRangeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("Attack_Type", 2);
        yield return new WaitForSeconds(0.6f);

        if (ivyProjectilePrefab != null && ivySpawnPoint != null)
        {
            GameObject ivy = Instantiate(ivyProjectilePrefab, ivySpawnPoint.position, Quaternion.identity);
            Vector2 dir = (player.position - transform.position).normalized;
            EntIvyProjectile projScript = ivy.GetComponent<EntIvyProjectile>();
            if (projScript != null) { projScript.damage = ivyDamage; projScript.direction = dir; }
        }

        yield return new WaitForSeconds(0.5f);
        anim.SetInteger("Attack_Type", 0);
        nextIvyTime = Time.time + ivyCooldown;
        isActionInProgress = false;
    }

    IEnumerator StunAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("Attack_Type", 3);
        // Èekáme na animaci, spawn øeší Animation Event
        yield return new WaitForSeconds(1.2f);
        anim.SetInteger("Attack_Type", 0);
        nextStunTime = Time.time + stunCooldown;
        isActionInProgress = false;
    }

    public void SpawnRootTrap() // Animation Event
    {
        if (rootTrapPrefab != null && player != null)
            Instantiate(rootTrapPrefab, player.position, Quaternion.identity);
    }
}