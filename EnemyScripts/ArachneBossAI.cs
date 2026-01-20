using UnityEngine;
using System.Collections;
using System;

public class ArachneBossAI : MonoBehaviour
{
    [Header("References")]
    public GameObject closterPrefab; // Minion
    public GameObject webPrefab;     // Projektil
    public Transform[] summonPoints; // Kde se rodí minioni

    [Header("Stats")]
    public float moveSpeed = 3f;
    public float meleeRange = 2.5f;

    [Header("Attacks")]
    public float attackCooldown = 3f;
    public int rapidFireCount = 5; // 5 ran za sebou
    public float burrowDuration = 2f; // Jak dlouho je pod zemí

    private Transform player;
    private EnemyHealth myHealth;
    private Animator anim;
    private Rigidbody2D rb;

    private float nextAttackTime;
    private bool isBusy = false; // Vykonává útok?

    // Fáze pro spawn (aby se nespustily víckrát)
    private bool spawned50 = false;
    private bool spawned25 = false;
    private bool spawned10 = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        myHealth = GetComponent<EnemyHealth>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        nextAttackTime = Time.time + 2f;
    }

    void Update()
    {
        if (player == null || myHealth.currentHealth <= 0) return;

        // 1. Kontrola Fází (Summoning) - Priorita è. 1
        CheckPhases();

        if (isBusy) return;

        // 2. Rozhodování co dìlat
        float dist = Vector3.Distance(transform.position, player.position);

        if (Time.time >= nextAttackTime)
        {
            // Náhodný výbìr útoku
            int rand = UnityEngine.Random.Range(0, 3); // 0, 1, 2

            if (dist < meleeRange)
            {
                StartCoroutine(MeleeAttack()); // Je blízko -> kousni
            }
            else
            {
                if (rand == 0) StartCoroutine(RapidFireWebs());
                else if (rand == 1) StartCoroutine(BurrowAttack());
                else ChasePlayer(); // Jen se pohni
            }
        }
        else
        {
            // Pokud má cooldown, jen pronásleduje (pokud není pod zemí)
            if (dist > meleeRange) ChasePlayer();
            else StopMoving();
        }
    }

    void CheckPhases()
    {
        float hpPercent = (float)myHealth.currentHealth / myHealth.maxHealth;

        if (hpPercent <= 0.5f && !spawned50)
        {
            spawned50 = true;
            StartCoroutine(SummonMinions(2)); // Na 50% vyvolej 2
        }
        else if (hpPercent <= 0.25f && !spawned25)
        {
            spawned25 = true;
            StartCoroutine(SummonMinions(3)); // Na 25% vyvolej 3
        }
        else if (hpPercent <= 0.1f && !spawned10)
        {
            spawned10 = true;
            StartCoroutine(SummonMinions(4)); // Na 10% vyvolej 4 (PANIKA!)
        }
    }

    // --- ÚTOKY ---

    IEnumerator MeleeAttack()
    {
        isBusy = true;
        StopMoving();
        anim.SetTrigger("Attack"); // Kousnutí
        yield return new WaitForSeconds(1f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    IEnumerator RapidFireWebs()
    {
        isBusy = true;
        StopMoving();
        // anim.SetTrigger("Shoot"); 

        for (int i = 0; i < rapidFireCount; i++)
        {
            if (webPrefab != null)
            {
                GameObject web = Instantiate(webPrefab, transform.position, Quaternion.identity);
                SmartWebProjectile script = web.GetComponent<SmartWebProjectile>();

                // Míøí na hráèe s malou odchylkou (aby to nebyla pøímka)
                Vector2 dir = (player.position - transform.position).normalized;
                float angle = UnityEngine.Random.Range(-15f, 15f); // Rozptyl
                dir = Quaternion.Euler(0, 0, angle) * dir;

                script.Launch(dir);
            }
            yield return new WaitForSeconds(0.2f); // Rychlá palba
        }

        yield return new WaitForSeconds(1f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown + 1f;
    }

    IEnumerator BurrowAttack()
    {
        isBusy = true;
        StopMoving();
        myHealth.isInvincible = true; // Pod zemí je nesmrtelná

        // anim.SetTrigger("Burrow"); // Animace zahrabání
        yield return new WaitForSeconds(0.5f);

        // Schováme sprita
        GetComponent<SpriteRenderer>().enabled = false;
        GetComponent<Collider2D>().enabled = false; // Vypneme kolize

        yield return new WaitForSeconds(burrowDuration);

        // Teleport pod hráèe
        transform.position = player.position;

        // Varování (volitelné - mohl bys tam dát particle efekt prachu)
        // ...

        // Vyleze ven
        GetComponent<SpriteRenderer>().enabled = true;
        GetComponent<Collider2D>().enabled = true;
        // anim.SetTrigger("Emerge");

        // Okamžitý útok po vylezení (Kousnutí)
        anim.SetTrigger("Attack");

        // Pokud je hráè blízko, dostane damage (jednoduchá verze)
        if (Vector3.Distance(transform.position, player.position) < 2f)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(20);
        }

        myHealth.isInvincible = false;
        yield return new WaitForSeconds(1f);
        isBusy = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    IEnumerator SummonMinions(int count)
    {
        // Toto má prioritu, pøeruší ostatní, pokud to jde
        isBusy = true;
        StopMoving();
        // anim.SetTrigger("Summon");
        Debug.Log("Arachne volá armádu!");

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < count; i++)
        {
            Transform spawnPoint = summonPoints[UnityEngine.Random.Range(0, summonPoints.Length)];
            Instantiate(closterPrefab, spawnPoint.position, Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
        }

        isBusy = false;
    }

    // --- POHYB ---
    void ChasePlayer()
    {
        Vector2 dir = (player.position - transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;
        anim.SetBool("IsMoving", true);

        // Otoèení sprite podle smìru
        if (dir.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else transform.localScale = new Vector3(-1, 1, 1);
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
        anim.SetBool("IsMoving", false);
    }
}