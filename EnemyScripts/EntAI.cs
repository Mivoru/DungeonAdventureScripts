using System;
using System.Collections;
using UnityEngine;

public class EntAI : BaseEnemyAI
{
    [Header("Combat Settings")]
    public float meleeRange = 2.5f;
    public float rangeAttackDistance = 6f; // Kdy zaène pouívat bøeèan

    [Header("Attacks")]
    public int meleeDamage = 20;
    public int ivyDamage = 35; // Silnı útok

    [Header("Cooldowns")]
    public float meleeCooldown = 2f;
    public float ivyCooldown = 5f;
    public float stunCooldown = 10f; // Elitní útok ménì èasto

    [Header("Prefabs")]
    public GameObject ivyProjectilePrefab; // Plazivı bøeèan
    public Transform ivySpawnPoint;        // Z ruky nebo ze zemì u nohou
    public GameObject rootTrapPrefab;      // Stunující koøeny

    // Èasovaèe
    private float nextMeleeTime;
    private float nextIvyTime;
    private float nextStunTime;

    public override void Start()
    {
        base.Start();
        // První stun mùe zkusit brzy
        nextStunTime = Time.time + 3f;
    }

    public override void Update()
    {
        base.Update(); // Øeší otáèení a animaci pohybu

        if (player == null || isActionInProgress)
        {
            if (agent.enabled) agent.isStopped = true;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // 1. Rozhodování útokù
        if (dist <= meleeRange)
        {
            if (Time.time >= nextMeleeTime) StartCoroutine(MeleeAttack());
        }
        else if (dist <= rangeAttackDistance)
        {
            // Jsme ve støední vzdálenosti -> Mùeme dát Bøeèan nebo Stun

            // Náhodná šance na Stun (pokud je ready)
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
                // Cooldowny bìí -> jdi k hráèi
                MoveToPlayer();
            }
        }
        else
        {
            // Moc daleko -> jdi k hráèi
            MoveToPlayer();
        }
    }

    void MoveToPlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // --- ÚTOKY ---

    IEnumerator MeleeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetTrigger("MeleeAttack"); // Animace úderu

        yield return new WaitForSeconds(0.5f); // Èas nápøahu

        // Jednoduchı melee zásah
        if (Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(meleeDamage);
        }

        yield return new WaitForSeconds(0.5f); // Dojezd animace

        nextMeleeTime = Time.time + meleeCooldown;
        isActionInProgress = false;
    }

    IEnumerator IvyRangeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetTrigger("RangeAttack"); // Animace zaboøení rukou

        // Èekáme na moment, kdy zaboøí ruce do zemì
        yield return new WaitForSeconds(0.6f);

        if (ivyProjectilePrefab != null && ivySpawnPoint != null)
        {
            // Vytvoøíme projektil
            GameObject ivy = Instantiate(ivyProjectilePrefab, ivySpawnPoint.position, Quaternion.identity);

            // Nasmìrujeme ho na hráèe
            Vector2 dir = (player.position - transform.position).normalized;

            EntIvyProjectile projScript = ivy.GetComponent<EntIvyProjectile>();
            if (projScript != null)
            {
                projScript.damage = ivyDamage;
                projScript.direction = dir;
            }
        }

        yield return new WaitForSeconds(0.5f); // Zvedání rukou

        nextIvyTime = Time.time + ivyCooldown;
        isActionInProgress = false;
    }

    IEnumerator StunAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;
        anim.SetTrigger("SummonAttack"); // Ruce nahoru

        // Chvíli "èaruje"
        yield return new WaitForSeconds(0.4f);

        if (rootTrapPrefab != null)
        {
            // Spawneme past PØESNÌ pod hráèem (v tu chvíli)
            // Hráè má pak chvilku na úhyb (øeší skript pasti)
            Instantiate(rootTrapPrefab, player.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(1.0f); // Dlouhá animace vyvolávání

        nextStunTime = Time.time + stunCooldown;
        isActionInProgress = false;
    }
}