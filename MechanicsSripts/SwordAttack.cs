using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float baseAttackRange = 1.5f;
    public float attackAngle = 100f;

    [Header("Timing & Speed")]
    public float baseAttackDuration = 0.3f; // Jak dlouho trvá švihnutí (pøi speed 1)
    public float attackCooldown = 0.5f;     // Pevný èas mezi útoky (nebo se mùže zkracovat speedem)
    public float attackSpeed = 1.0f;        // Multiplikátor rychlosti (napø. ze statistik)

    [Header("Weapon Stats")]
    public int weaponDamage = 20;
    public float damageMultiplier = 1f;

    [Header("Layers")]
    public LayerMask enemyLayers;
    public LayerMask obstacleLayers;

    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private Quaternion defaultRotation;

    void Start()
    {
        defaultRotation = transform.localRotation;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // Útoèíme jen, pokud už neútoèíme A uplynul èas cooldownu
        if (context.performed && !isAttacking && Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // Spustíme animaci na hráèi (Trigger)
        Animator playerAnim = GetComponentInParent<Animator>();
        if (playerAnim != null)
        {
            // Mùžeme poslat i rychlost animace do animátoru
            playerAnim.SetFloat("AttackSpeed", attackSpeed);
            playerAnim.SetTrigger("Attack");
        }

        // Aktualizujeme pozici hitboxu podle smìru pohledu hráèe
        UpdateAttackPointPosition(playerAnim);

        // Výpoèet trvání útoku podle rychlosti (èím vyšší speed, tím kratší duration)
        float currentDuration = baseAttackDuration / attackSpeed;

        // Nastavení dalšího útoku (Cooldown)
        // Možnost A: Pevný cooldown (napø. 0.5s)
        // nextAttackTime = Time.time + attackCooldown;

        // Možnost B: Cooldown se zkracuje s rychlostí útoku (vhodné pro RPG)
        float currentCooldown = attackCooldown / attackSpeed;
        nextAttackTime = Time.time + currentCooldown;

        // Èekání na "zásah" (napø. v polovinì švihu)
        yield return new WaitForSeconds(currentDuration * 0.3f);

        // Udìlení poškození
        DealConeDamage();

        // Èekání na zbytek animace
        yield return new WaitForSeconds(currentDuration * 0.7f);

        // Reset
        // Pokud otáèíš meèem manuálnì (bez animátoru), resetuj rotaci zde
        // transform.localRotation = defaultRotation; 

        isAttacking = false;
    }

    void DealConeDamage()
    {
        // ... (Stejná logika jako pøedtím) ...

        // 1. Zjistíme støed útoku (posunutý pøed hráèe)
        // Získáme smìr pohledu z Animátoru nebo Transformu
        Vector2 facingDir = transform.parent.up; // Defaultnì nahoru, pokud se hráè netoèí

        // Pokud máš v animátoru LastHorizontal/Vertical, použij je:
        Animator anim = GetComponentInParent<Animator>();
        if (anim != null)
        {
            facingDir = new Vector2(anim.GetFloat("LastHorizontal"), anim.GetFloat("LastVertical")).normalized;
            if (facingDir == Vector2.zero) facingDir = new Vector2(0, -1); // Fallback dolù
        }

        Vector3 attackCenter = transform.parent.position + (Vector3)facingDir * 0.5f; // Posuneme kruh

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, baseAttackRange, enemyLayers);

        foreach (Collider2D hit in hits)
        {
            // Raycast pro kontrolu zdí
            Vector2 dirToEnemy = (hit.transform.position - transform.parent.position).normalized;
            float dist = Vector2.Distance(transform.parent.position, hit.transform.position);

            if (!Physics2D.Raycast(transform.parent.position, dirToEnemy, dist, obstacleLayers))
            {
                // Aplikace damage
                EnemyStats eStats = hit.GetComponent<EnemyStats>();
                if (eStats != null)
                {
                    int totalDamage = Mathf.RoundToInt(weaponDamage * damageMultiplier);
                    eStats.TakeDamage(totalDamage);
                }
            }
        }
    }

    void UpdateAttackPointPosition(Animator anim)
    {
        // Pokud používáš vizuální AttackPoint pro Debug, posuò ho
        if (anim != null)
        {
            float x = anim.GetFloat("LastHorizontal");
            float y = anim.GetFloat("LastVertical");
            transform.localPosition = new Vector3(x, y, 0).normalized * 0.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (transform.parent != null)
        {
            Gizmos.color = Color.red;
            // Vykreslíme kruh tam, kde by byl pøi útoku (zhruba)
            Gizmos.DrawWireSphere(transform.position, baseAttackRange);
        }
    }
}