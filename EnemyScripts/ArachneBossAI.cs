using UnityEngine;
using System.Collections;

public class ArachneBossAI : MonoBehaviour
{
    [Header("References")]
    public GameObject closterPrefab; // Minion
    public GameObject webPrefab;     // Projektil

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float meleeRange = 2.5f;

    [Header("Attacks")]
    public float attackCooldown = 3f;
    public int rapidFireCount = 5;
    public int meleeDamage = 20;

    [Header("Summon Settings")]
    public float summonDistance = 5f; // Vzdálenost spawnu od bosse
    public LayerMask obstacleMask;    // Vrstva zdí (Wall/Obstacles)

    [Header("Burrow Settings (Animace)")]
    public float burrowDuration = 2f;      // Jak dlouho je neviditelná pod zemí
    public float burrowDownDuration = 0.8f; // Délka animace ArachneTeleportDown
    public float burrowUpDuration = 0.8f;   // Délka animace ArachneTeleportUp

    private Transform player;

    // --- ZMÌNA: Používáme EnemyStats ---
    private EnemyStats myStats;

    private Animator anim;
    private Rigidbody2D rb;

    private float nextAttackTime;
    private bool isBusy = false;

    // Fáze
    private bool spawned50 = false;
    private bool spawned25 = false;
    private bool spawned10 = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // --- ZMÌNA: Naèítáme EnemyStats ---
        myStats = GetComponent<EnemyStats>();

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        nextAttackTime = Time.time + 2f;

        if (myStats == null) Debug.LogError("CHYBA: Bossovi chybí komponenta EnemyStats!");
    }

    void Update()
    {
        // --- ZMÌNA: Kontrola pøes myStats ---
        if (player == null || myStats == null || myStats.currentHealth <= 0) return;

        CheckPhases();

        // 1. Pokud probíhá Coroutina (útok), nedìlej nic.
        if (isBusy) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // 2. LOGIKA ÚTOKU
        if (Time.time >= nextAttackTime)
        {
            // Útoèíme jenom, pokud je Animator pøipravený
            if (IsReadyToAttack())
            {
                int rand = UnityEngine.Random.Range(0, 3);

                if (dist < meleeRange)
                {
                    StartCoroutine(MeleeAttack());
                }
                else
                {
                    if (rand == 0) StartCoroutine(RapidFireWebs());
                    else if (rand == 1) StartCoroutine(BurrowAttack());
                    else ChasePlayer();
                }
            }
            else
            {
                StopMoving();
            }
        }
        else
        {
            // 3. LOGIKA POHYBU (Cooldown)
            if (dist > meleeRange) ChasePlayer();
            else StopMoving();
        }
    }

    bool IsReadyToAttack()
    {
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // Jsme v pøechodu? -> NEÚTOÈIT
        if (anim.IsInTransition(0)) return false;

        // Jsme v bezpeèném stavu? (Idle nebo Walk)
        if (stateInfo.IsName("ArachneIdle") || stateInfo.IsName("ArachneWalk"))
        {
            return true;
        }
        return false;
    }

    void CheckPhases()
    {
        // --- ZMÌNA: Výpoèet HP pøes myStats ---
        if (myStats.maxHealth == 0) return;

        float hpPercent = (float)myStats.currentHealth / myStats.maxHealth;

        if (hpPercent <= 0.5f && !spawned50)
        {
            // Debug.Log("SPPOUŠTÍM FÁZI 1 (50%) - SUMMON!");
            spawned50 = true;
            StartCoroutine(SummonMinions(2));
        }
        else if (hpPercent <= 0.25f && !spawned25)
        {
            // Debug.Log("SPPOUŠTÍM FÁZI 2 (25%) - SUMMON!");
            spawned25 = true;
            StartCoroutine(SummonMinions(2));
        }
        else if (hpPercent <= 0.1f && !spawned10)
        {
            // Debug.Log("SPPOUŠTÍM FÁZI 3 (10%) - SUMMON!");
            spawned10 = true;
            StartCoroutine(SummonMinions(2));
        }
    }

    // --- LOGIKA SPAWNOVÁNÍ ---
    IEnumerator SummonMinions(int count)
    {
        isBusy = true;
        StopMoving();

        // Pojistka pro animátor
        anim.ResetTrigger("Attack");
        anim.SetTrigger("Summon");

        yield return new WaitForSeconds(1f);

        float angleStep = 360f / count;
        float startAngle = UnityEngine.Random.Range(0f, 360f);

        for (int i = 0; i < count; i++)
        {
            float currentAngle = startAngle + (i * angleStep);
            Vector3 spawnPos = GetValidSpawnPosition(currentAngle);

            // FALLBACK
            if (spawnPos == Vector3.zero)
            {
                spawnPos = transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 2f;
            }

            if (closterPrefab != null)
            {
                GameObject minion = Instantiate(closterPrefab, spawnPos, Quaternion.identity);
                if (player != null)
                {
                    Vector3 dir = player.position - minion.transform.position;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    minion.transform.rotation = Quaternion.Euler(0, 0, angle + 90);
                }
            }
            yield return new WaitForSeconds(0.3f);
        }
        isBusy = false;
    }

    Vector3 GetValidSpawnPosition(float angleDegrees)
    {
        int maxAttempts = 10;
        float currentAngle = angleDegrees;

        for (int i = 0; i < maxAttempts; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector3 targetPos = transform.position + (Vector3)(dir * summonDistance);

            Collider2D hit = Physics2D.OverlapCircle(targetPos, 0.5f, obstacleMask);
            if (hit == null) return targetPos;

            currentAngle += 30f;
        }
        return Vector3.zero;
    }

    IEnumerator BurrowAttack()
    {
        isBusy = true;
        StopMoving();

        // --- ZMÌNA: Invincibility pøes myStats ---
        // (Ujisti se, že máš v EnemyStats 'public bool isInvincible = false;')
        myStats.isInvincible = true;

        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Emerge");
        anim.ResetTrigger("Summon");
        anim.ResetTrigger("Shoot");
        anim.ResetTrigger("Burrow");

        anim.SetTrigger("Burrow");
        yield return new WaitForSeconds(burrowDownDuration + 0.1f);

        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false;

        yield return new WaitForSeconds(burrowDuration);

        if (player != null) transform.position = player.position;

        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;

        anim.SetTrigger("Emerge");

        yield return new WaitForSeconds(burrowUpDuration + 0.1f);

        anim.ResetTrigger("Emerge");
        anim.SetTrigger("Attack");

        if (player != null && Vector3.Distance(transform.position, player.position) < 2.5f)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(meleeDamage + 5);
            }
        }

        // --- ZMÌNA: Vypnutí invincibility ---
        myStats.isInvincible = false;

        yield return new WaitForSeconds(1f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    IEnumerator MeleeAttack()
    {
        isBusy = true;
        StopMoving();
        anim.SetTrigger("Attack");
        yield return new WaitForSeconds(0.5f);

        if (Vector3.Distance(transform.position, player.position) <= meleeRange + 0.5f)
        {
            PlayerStats stats = player.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDamage(meleeDamage);
                stats.ApplyPoison(3f, 5);
            }
        }
        yield return new WaitForSeconds(0.5f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    IEnumerator RapidFireWebs()
    {
        isBusy = true;
        StopMoving();
        anim.SetTrigger("Shoot");

        for (int i = 0; i < rapidFireCount; i++)
        {
            if (webPrefab != null)
            {
                GameObject web = Instantiate(webPrefab, transform.position, Quaternion.identity);
                SmartWebProjectile script = web.GetComponent<SmartWebProjectile>();

                if (script != null)
                {
                    Vector2 dir = (player.position - transform.position).normalized;
                    float angle = UnityEngine.Random.Range(-15f, 15f);
                    dir = Quaternion.Euler(0, 0, angle) * dir;
                    script.Launch(dir);
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(1f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown + 1f;
    }

    void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
        anim.SetBool("IsMoving", true);

        if (dir.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else transform.localScale = new Vector3(-1, 1, 1);
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }
}