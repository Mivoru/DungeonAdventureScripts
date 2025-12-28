using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GiantAI : BaseEnemyAI
{
    [Header("Boss Settings")]
    public float meleeRange = 3.5f;
    public float throwRangeMax = 12f;
    public float throwRangeMin = 5f;

    [Header("Cooldowns")]
    public float slapCooldown = 2f;
    public float slamCooldown = 6f;
    public float throwCooldown = 5f;
    public float summonCooldown = 12f;

    [Header("Damage Settings")]
    public int slapDamage = 15;
    public int slamDamage = 30;
    public int throwRockDamage = 20;
    public int rollingRockDamage = 25;

    [Header("Projectile Settings")]
    public float throwRockSpeed = 15f;
    public float minRollingSpeed = 6f;
    public float maxRollingSpeed = 10f;

    [Header("Attacks Prefabs")]
    public GameObject rockPrefab;
    public Transform throwPoint;
    public GameObject rollingRockPrefab;
    public Transform[] summonPoints;

    [Header("Area Damage (Slam)")]
    public float slamRadius = 5f;
    public LayerMask playerLayer;

    // Èasovaèe a stavy
    private float nextActionTime;
    private bool phase50 = false;
    private bool phase25 = false;
    private bool phase05 = false;
    private bool isInvulnerable = false;

    private int[] meleePattern = { 1, 1, 2 };

    public override void Start()
    {
        base.Start();
        // Malá prodleva na startu
        nextActionTime = Time.time + 2f;
    }

    // --- OPRAVA 1: RESET PØI STUNU ---
    // Když EnemyStats vypne tento skript (Hit/Stun), musíme vyèistit stav
    void OnDisable()
    {
        isActionInProgress = false;
        if (agent != null) agent.isStopped = false;
        StopAllCoroutines(); // Pro jistotu
    }

    public override void Update()
    {
        base.Update();

        if (player == null || isActionInProgress) return;

        // 1. FÁZE (Priorita)
        float hpPercent = (float)stats.currentHealth / stats.maxHealth;
        if (!phase50 && hpPercent <= 0.5f) { StartCoroutine(SummonPhase(50)); return; }
        if (!phase25 && hpPercent <= 0.25f) { StartCoroutine(SummonPhase(25)); return; }
        if (!phase05 && hpPercent <= 0.05f) { StartCoroutine(SummonPhase(5)); return; }

        float dist = Vector2.Distance(transform.position, player.position);

        // 2. COOLDOWN & POHYB (OPRAVA CHOVÁNÍ)
        if (Time.time < nextActionTime)
        {
            // Pokud jsme v cooldownu, ale hráè je DALEKO -> Jdeme k nìmu
            if (dist > meleeRange)
            {
                agent.isStopped = false;
                agent.SetDestination(player.position);
            }
            else
            {
                // Pokud jsme v cooldownu a hráè je BLÍZKO -> STÙJ A ÈEKEJ (netlaè se do nìj)
                agent.isStopped = true;
                agent.velocity = Vector2.zero;
                // RotateTowards se volá v base.Update(), takže se na hráèe bude stále otáèet
            }
            return; // Èekáme na nabití útoku
        }

        // 3. ROZHODOVÁNÍ ÚTOKÙ (Když není cooldown)
        if (dist <= meleeRange)
        {
            PerformMeleeAttack();
        }
        else if (dist <= throwRangeMax && dist > throwRangeMin)
        {
            StartCoroutine(ThrowRoutine());
        }
        else
        {
            // Hráè je daleko nebo v "hluchém místì" (mezi melee a throw) -> Jdi k nìmu
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
    }

    void PerformMeleeAttack()
    {
        int attackType = meleePattern[UnityEngine.Random.Range(0, meleePattern.Length)];
        if (attackType == 1) StartCoroutine(SlapRoutine());
        else StartCoroutine(SlamRoutine());
    }

    // --- COROUTINES ---

    IEnumerator SlapRoutine()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("AttackType", 1); // Spustí animaci

        // Zde èekáme na animaci. Pokud tì nìkdo trefí a dá Stun, 
        // zavolá se OnDisable() a tato Coroutine se bezpeènì ukonèí.
        yield return new WaitForSeconds(1.0f);

        anim.SetInteger("AttackType", 0); // Konec animace
        isActionInProgress = false;
        nextActionTime = Time.time + slapCooldown;
    }

    IEnumerator SlamRoutine()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("AttackType", 2);
        yield return new WaitForSeconds(1.5f);
        anim.SetInteger("AttackType", 0);
        isActionInProgress = false;
        nextActionTime = Time.time + slamCooldown;
    }

    IEnumerator ThrowRoutine()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetInteger("AttackType", 3);
        yield return new WaitForSeconds(1.2f);
        anim.SetInteger("AttackType", 0);
        isActionInProgress = false;
        nextActionTime = Time.time + throwCooldown;
    }

    IEnumerator SummonPhase(int percent)
    {
        if (percent == 50) phase50 = true;
        if (percent == 25) phase25 = true;
        if (percent == 5) phase05 = true;

        isActionInProgress = true;
        isInvulnerable = true;
        agent.isStopped = true;

        anim.SetInteger("AttackType", 4);

        // Spawn kamenù
        for (int i = 0; i < 8; i++)
        {
            SpawnRollingRock();
            yield return new WaitForSeconds(0.6f);
        }

        anim.SetInteger("AttackType", 0);
        isInvulnerable = false;
        isActionInProgress = false;
        nextActionTime = Time.time + 2f;
    }

    // --- SPAWNOVÁNÍ KAMENÙ ---

    void SpawnRollingRock()
    {
        Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 spawnPos = player.position + (Vector3)randomDir * UnityEngine.Random.Range(6f, 8f);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            if (rollingRockPrefab)
            {
                GameObject rock = Instantiate(rollingRockPrefab, hit.position, Quaternion.identity);
                Vector3 dirToPlayer = (player.position - hit.position).normalized;

                GiantProjectile gp = rock.GetComponent<GiantProjectile>();
                if (gp != null)
                {
                    gp.isRolling = true;
                    // Nastavení rychlosti
                    gp.speed = UnityEngine.Random.Range(minRollingSpeed, maxRollingSpeed);
                    // Nastavení poškození
                    gp.damage = rollingRockDamage;

                    gp.Initialize(dirToPlayer);
                }
            }
        }
    }

    // --- ANIMATION EVENTS ---

    public void TriggerAttackEffect()
    {
        int type = anim.GetInteger("AttackType");

        switch (type)
        {
            case 1: // SLAP
                // Použijeme promìnnou slapDamage
                DealDamageInCircle(meleeRange, slapDamage);
                break;

            case 2: // SLAM
                // Použijeme promìnnou slamDamage
                DealDamageInCircle(slamRadius, slamDamage);
                break;

            case 3: // THROW
                if (rockPrefab && throwPoint)
                {
                    GameObject rock = Instantiate(rockPrefab, throwPoint.position, Quaternion.identity);
                    Vector3 dir = (player.position - throwPoint.position).normalized;

                    GiantProjectile gp = rock.GetComponent<GiantProjectile>();
                    if (gp != null)
                    {
                        gp.speed = throwRockSpeed;
                        // Nastavení poškození pro hozený kámen
                        gp.damage = throwRockDamage;

                        gp.Initialize(dir);
                    }
                }
                break;
        }
    }

    void DealDamageInCircle(float radius, int dmg)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, playerLayer);
        foreach (var hit in hits)
        {
            hit.GetComponent<PlayerStats>()?.TakeDamage(dmg);
        }
    }

    public bool IsInvulnerable() { return isInvulnerable; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, meleeRange);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(transform.position, slamRadius);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, throwRangeMax);
    }
}