using System.Collections;
using UnityEngine;

public class EntAI : BaseEnemyAI
{
    [Header("Combat Settings")]
    public float meleeRange = 2.5f;
    public float rangeAttackDistance = 6f;

    [Header("Attacks")]
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

    // Èasovaèe
    private float nextMeleeTime;
    private float nextIvyTime;
    private float nextStunTime;

    public override void Start()
    {
        base.Start();
        nextStunTime = Time.time + 3f;
    }

    public override void Update()
    {
        base.Update(); // Base øeší Speed pro Walk/Idle

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
                MoveToPlayer();
            }
        }
        else
        {
            MoveToPlayer();
        }
    }

    void MoveToPlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // --- ÚTOKY (Coroutines) ---

    IEnumerator MeleeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;

        // Spuštìní animace Melee (Int 1)
        anim.SetInteger("Attack_Type", 1);

        // Èekáme na moment úderu (uprav podle délky animace)
        yield return new WaitForSeconds(0.5f);

        if (Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
        {
            player.GetComponent<PlayerStats>()?.TakeDamage(meleeDamage);
        }

        // Èekáme na konec animace
        yield return new WaitForSeconds(0.5f);

        // Reset do Idle/Walk
        anim.SetInteger("Attack_Type", 0);

        nextMeleeTime = Time.time + meleeCooldown;
        isActionInProgress = false;
    }

    IEnumerator IvyRangeAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;

        // Spuštìní animace Range (Int 2)
        anim.SetInteger("Attack_Type", 2);

        // Èekáme na moment vystøelení (kdy zaboøí ruce)
        yield return new WaitForSeconds(0.6f);

        if (ivyProjectilePrefab != null && ivySpawnPoint != null)
        {
            GameObject ivy = Instantiate(ivyProjectilePrefab, ivySpawnPoint.position, Quaternion.identity);
            Vector2 dir = (player.position - transform.position).normalized;

            EntIvyProjectile projScript = ivy.GetComponent<EntIvyProjectile>();
            if (projScript != null)
            {
                projScript.damage = ivyDamage;
                projScript.direction = dir;
            }
        }

        yield return new WaitForSeconds(0.5f); // Dojezd animace

        // Reset
        anim.SetInteger("Attack_Type", 0);

        nextIvyTime = Time.time + ivyCooldown;
        isActionInProgress = false;
    }

    IEnumerator StunAttack()
    {
        isActionInProgress = true;
        agent.isStopped = true;

        // Spuštìní animace Summon (Int 3)
        anim.SetInteger("Attack_Type", 3);

        // ZDE NEÈEKÁME NA CÓDÌ! 
        // Èekáme jen na dokonèení celé animace.
        // Samotný spawn pasti vyvolá Animation Event (metoda SpawnRootTrap níže).

        yield return new WaitForSeconds(1.2f); // Celková délka animace vyvolávání

        // Reset
        anim.SetInteger("Attack_Type", 0);

        nextStunTime = Time.time + stunCooldown;
        isActionInProgress = false;
    }

    // --- TUTO METODU ZAVOLÁŠ V ANIMACI (Animation Event) ---
    public void SpawnRootTrap()
    {
        if (rootTrapPrefab != null && player != null)
        {
            // Spawneme past PØESNÌ pod hráèem
            Instantiate(rootTrapPrefab, player.position, Quaternion.identity);
        }
    }
}