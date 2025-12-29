using System;
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

    [Header("Search Logic")]
    public float searchDuration = 5f; // Jak dlouho hledá, než se vrátí na hlídku
    public float searchRadius = 8f;   // V jakém okruhu "čmuchá"

    [Header("Vision & Aggro")]
    public float aggroRange = 10f;
    public LayerMask obstacleLayer;   // Vrstva zdí (aby neviděl skrz)

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

    [Header("Panic Evasion Settings")]
    public float panicDistance = 4f;    // Přejmenováno z evadeDistance, aby to sedělo s Update
    public float timeToPanic = 1.0f;    // Přejmenováno z timeToEvade
    private float closeRangeTimer = 0f;

    // --- STAVY ---
    private enum State { Patrol, Chase, Shoot, Evade, Search, Flee }
    private State currentState = State.Patrol;

    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;
    private Vector3 startPosition;
    private Vector3 lastKnownPosition;
    private Vector3 baseScale;

    private float nextShootTime;
    private float nextEvadeTime;
    private float patrolTimer;
    private float searchTimer;

    private bool isActionInProgress = false;
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
        if (isActionInProgress) return;

        // Spočítáme vzdálenost HNED na začátku, protože ji potřebujeme pro více podmínek
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        bool canSeePlayer = CheckLineOfSight();

        // 1. KONTROLA ÚHYBU PŘED PROJEKTILY (Priorita - uhýbá před šípy)
        if (Time.time >= nextEvadeTime && CheckForProjectiles())
        {
            // Resetujeme timer paniky, protože už uhýbáme kvůli šípu
            closeRangeTimer = 0f;
            StartCoroutine(PerformEvasion());
            return;
        }

        // 1.5. KONTROLA PANIKY (Hráč je moc dlouho blízko)
        // Pokud je hráč blíž než "panicDistance"
        if (distToPlayer < panicDistance)
        {
            // Přičítáme čas
            closeRangeTimer += Time.deltaTime;

            // Pokud jsme u něj déle než 1s A ZÁROVEŇ nemáme cooldown na úskok
            if (closeRangeTimer >= timeToPanic && Time.time >= nextEvadeTime)
            {
                closeRangeTimer = 0f; // Reset časovače
                StartCoroutine(PerformEvasion()); // Spustíme stejný úskok
                return; // Ukončíme Update, ať neřeší střelbu/chůzi
            }
        }
        else
        {
            // Hráč odešel z nebezpečné zóny -> resetujeme počítadlo
            closeRangeTimer = 0f;
        }

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
                SearchLogic(canSeePlayer);
                break;
        }

        // Otáčení (Face target)
        if (currentState != State.Patrol && currentState != State.Search)
        {
            RotateTowards(player.position);
        }
        else if (agent.velocity.sqrMagnitude > 0.1f)
        {
            // Při hlídce/hledání se otáčí tam, kam jde
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

        // Pokud je blízko A ZÁROVEŇ vidí hráče (není za zdí)
        if (dist < aggroRange && see)
        {
            currentState = State.Chase;
        }
    }

    void ChaseLogic(float dist, bool see)
    {
        agent.speed = runSpeed;

        if (see)
        {
            // Vidíme hráče -> aktualizujeme jeho pozici
            lastKnownPosition = player.position;

            if (dist < fleeDistance)
                currentState = State.Flee;
            else if (dist <= idealRange)
                currentState = State.Shoot;
            else
                agent.SetDestination(player.position);
        }
        else
        {
            // Ztratili jsme vizuální kontakt -> Jdeme hledat
            currentState = State.Search;
        }
    }

    void ShootLogic(float dist, bool see)
    {
        // Zastavíme pohyb, abychom mohli mířit
        agent.isStopped = true;
        agent.velocity = Vector2.zero;

        // 1. ZTRÁTA VIDITELNOSTI -> HLEDAT
        if (!see)
        {
            currentState = State.Search;
            agent.isStopped = false;
            return;
        }

        // 2. MOC DALEKO -> PRONÁSLEDOVAT
        if (dist > idealRange * 1.2f)
        {
            currentState = State.Chase;
            agent.isStopped = false;
            return;
        }

        // 3. STŘELBA (HLAVNÍ ZMĚNA - PRIORITA)
        // Zjistíme, jestli máme nabito
        bool readyToShoot = Time.time >= nextShootTime;

        if (readyToShoot)
        {
            // I když je hráč u nás (dist < fleeDistance),
            // raději mu dáme ránu, než začneme utíkat.
            StartCoroutine(ShootRoutine());

            // Nastavíme cooldown (přidal jsem malou náhodu, ať to není strojové)
            nextShootTime = Time.time + shootCooldown + UnityEngine.Random.Range(-0.1f, 0.1f);

            return; // Vystřelili jsme, v tomto framu už nic jiného neřešíme
        }

        // 4. ÚTĚK (Až když nemůžeme střílet)
        // Pokud máme cooldown A ZÁROVEŇ je hráč moc blízko, tak utíkáme.
        if (dist < fleeDistance)
        {
            currentState = State.Flee;
            agent.isStopped = false;
            return;
        }
    }

    void FleeLogic(float dist)
    {
        agent.speed = runSpeed;

        // Pokud už utekl do bezpečí, vrací se do boje
        if (dist > fleeDistance * 1.5f)
        {
            currentState = State.Chase;
            return;
        }

        // Logika ústupu (Smart Flee)
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        Vector3 targetFleePos = transform.position + dirFromPlayer * 4f;
        NavMeshHit hit;

        // Zkusí utéct dozadu
        if (!NavMesh.SamplePosition(targetFleePos, out hit, 2.0f, NavMesh.AllAreas))
        {
            // Pokud nemůže dozadu (zeď), zkusí do boku
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

    void SearchLogic(bool see)
    {
        // 1. Pokud ho zmerčíme, okamžitě Chase
        if (see)
        {
            currentState = State.Chase;
            searchTimer = 0;
            return;
        }

        agent.speed = runSpeed;

        // 2. Běžíme na místo posledního spatření
        if (Vector2.Distance(transform.position, lastKnownPosition) > 2f && searchTimer == 0)
        {
            agent.SetDestination(lastKnownPosition);
        }
        else
        {
            // 3. Jsme na místě -> začínáme prohledávat okolí (náhodná chůze v searchRadius)
            searchTimer += Time.deltaTime;

            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector2 randomPoint = UnityEngine.Random.insideUnitCircle * searchRadius;
                Vector3 searchDest = lastKnownPosition + new Vector3(randomPoint.x, randomPoint.y, 0);

                NavMeshHit hit;
                if (NavMesh.SamplePosition(searchDest, out hit, 2f, NavMesh.AllAreas))
                {
                    agent.SetDestination(hit.position);
                }
            }

            // 4. Vypršel čas hledání -> Návrat na hlídku
            if (searchTimer > searchDuration)
            {
                currentState = State.Patrol;
                SetPatrolPoint();
                searchTimer = 0;
            }
        }
    }

    // --- POMOCNÉ FUNKCE A DETEKCE ---

    // Toto nahrazuje původní HasLineOfSight, přidává kontrolu zdí
    bool CheckLineOfSight()
    {
        Vector2 dir = player.position - transform.position;
        // Optimalizace: Pokud je dál než AggroRange, rovnou false
        if (dir.magnitude > aggroRange) return false;

        // Raycast na ObstacleLayer
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, aggroRange, obstacleLayer);

        // Pokud paprsek trefí collider (zeď) a ten collider není hráč, tak nevidíme
        if (hit.collider != null && hit.transform != player) return false;

        return true;
    }

    bool CheckForProjectiles()
    {
        return Physics2D.OverlapCircle(transform.position, 4f, projectileLayer) != null;
    }

    // Voláno externě (např. při zásahu šípem)
    public void TriggerAggro()
    {
        lastKnownPosition = player.position;
        // I když ho nevidí, přepne do Chase (poběží na lastKnownPosition, pak případně Search)
        currentState = State.Chase;
    }

    // --- AKCE (COROUTINES) ---

    IEnumerator ShootRoutine()
    {
        isActionInProgress = true;
        anim.SetTrigger("Shoot");

        // Čekáme na animaci (Animation Event zavolá ShootProjectile)
        yield return new WaitForSeconds(1.0f);

        isActionInProgress = false;
    }

    IEnumerator PerformEvasion()
    {
        isActionInProgress = true;
        isEvading = true;

        // 1. Zastavíme standardní pohyb
        agent.isStopped = true;
        agent.ResetPath();
        agent.velocity = Vector2.zero;

        // 2. Animace
        anim.SetTrigger("Evade");

        // 3. Výpočet směru (Od hráče)
        Vector3 dirFromPlayer = (transform.position - player.position).normalized;
        float dashDistance = 5f; // Prodloužíme úskok
        float dashTime = 0.25f;  // Zrychlíme úskok (aby mohl dřív střílet)

        // 4. KONTROLA ZDI (Raycast na NavMeshi)
        // Zkusíme uskočit dozadu. Pokud je tam zeď, zkusíme doprava/doleva.
        Vector3 targetPos = transform.position + (dirFromPlayer * dashDistance);
        NavMeshHit hit;

        // NavMesh.Raycast vrátí true, pokud narazí do "zdi" v NavMeshi
        if (NavMesh.Raycast(transform.position, targetPos, out hit, NavMesh.AllAreas))
        {
            // Cesta dozadu je blokovaná! Zkusíme uskočit do boku.
            Vector3 sideDir = Vector3.Cross(dirFromPlayer, Vector3.forward); // Kolmice
            targetPos = transform.position + (sideDir * dashDistance);

            // Zkontrolujeme i bok
            if (NavMesh.Raycast(transform.position, targetPos, out hit, NavMesh.AllAreas))
            {
                // I bok je blokovaný -> skočíme jen kousek (tam, kde jsme narazili)
                targetPos = hit.position;
            }
        }

        // 5. Samotný pohyb (Lerp)
        Vector3 startPos = transform.position;
        float timer = 0f;

        while (timer < dashTime)
        {
            float t = timer / dashTime;
            // Použijeme lineární pohyb, je to pro dash čitelnější
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            timer += Time.deltaTime;
            yield return null;
        }

        // Pojistka: Ujistíme se, že skončil přesně v cíli
        transform.position = targetPos;

        // 6. Reset
        agent.isStopped = false;

        // Trik: Okamžitě se otočíme na hráče, abychom mohli hned střílet
        RotateTowards(player.position);

        nextEvadeTime = Time.time + evadeCooldown;
        isActionInProgress = false;
        isEvading = false;

        // Pokud jsme dostatečně daleko, přepneme rovnou do střelby (ne Chase)
        if (Vector2.Distance(transform.position, player.position) <= idealRange)
        {
            currentState = State.Shoot;
        }
    }

    // --- EVENTY Z ANIMACÍ ---

    // Tuto metodu volá Animation Event v animaci střelby
    public void ShootProjectile()
    {
        if (arrowPrefab == null || firePoint == null || player == null) return;

        Vector2 direction = (player.position - firePoint.position).normalized;
        GameObject arrow = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);

        // Nastavení rychlosti šípu
        Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
        // Poznámka: V Unity 6 se používá linearVelocity, ve starších velocity
        if (rb != null) rb.linearVelocity = direction * arrowSpeed;

        ArrowProjectile proj = arrow.GetComponent<ArrowProjectile>();
        if (proj != null) proj.damage = bowDamage;
    }

    // --- OSTATNÍ ---

    public bool IsEvading()
    {
        return isEvading;
    }

    void SetPatrolPoint()
    {
        // Najde náhodný bod kolem startovní pozice
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
        // Otočení spritu pomocí Scale X
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, idealRange);

        Gizmos.color = Color.cyan;
        if (currentState == State.Search) Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);
    }
}