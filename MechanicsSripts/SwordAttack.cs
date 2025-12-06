using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 1.5f;  // Dosah útoku
    public float attackAngle = 100f;  // Úhel výseèe

    [Header("Timing")]
    public float baseAttackDuration = 0.3f; // Základní délka animace
    public float attackCooldown = 0.5f;     // Základní èas mezi útoky

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

    // Voláno z WeaponManageru
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && !isAttacking && Time.time >= nextAttackTime)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // 1. Získání rychlosti útoku (z PlayerStats nebo základní 1.0)
        float currentAttackSpeed = 1.0f;
        PlayerStats stats = GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            currentAttackSpeed = stats.attackSpeed;
        }

        // 2. Spuštìní animace
        Animator playerAnim = GetComponentInParent<Animator>();
        if (playerAnim != null)
        {
            // Mùžeme poslat rychlost do animátoru (pokud máš parametr AttackSpeed)
            playerAnim.SetFloat("AttackSpeed", currentAttackSpeed);
            playerAnim.SetTrigger("Attack");
        }

        // 3. Aktualizace pozice hitboxu (podle toho kam koukáme)
        UpdateAttackPointPosition(playerAnim);

        // Výpoèet trvání (rychlejší útok = kratší èas)
        float duration = baseAttackDuration / currentAttackSpeed;

        // Nastavení cooldownu
        float cooldown = attackCooldown / currentAttackSpeed;
        nextAttackTime = Time.time + cooldown;

        // Èekání na "zásah" (v cca 30% animace)
        yield return new WaitForSeconds(duration * 0.3f);

        // 4. Udìlení poškození
        DealConeDamage();

        // Zbytek animace
        yield return new WaitForSeconds(duration * 0.7f);

        isAttacking = false;
    }

    void DealConeDamage()
    {
        // A) Výpoèet poškození
        PlayerStats stats = GetComponentInParent<PlayerStats>();
        int finalDamage = (stats != null) ? stats.GetCalculatedDamage(weaponDamage) : weaponDamage;

        // B) Zjištìní smìru (z Animátoru, protože transform se netoèí)
        Vector2 facingDir = Vector2.down;

        Animator anim = GetComponentInParent<Animator>();
        if (anim != null)
        {
            facingDir = new Vector2(anim.GetFloat("LastHorizontal"), anim.GetFloat("LastVertical"));
            if (facingDir.sqrMagnitude > 0.01f) facingDir.Normalize();
            else facingDir = Vector2.down;
        }

        // C) Støed útoku
        Vector3 attackCenter = transform.parent.position + (Vector3)facingDir * 0.5f;

        // D) Detekce a Zásah
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRange, enemyLayers);

        foreach (Collider2D hit in hits)
        {
            Vector2 dirToEnemy = (hit.transform.position - transform.parent.position).normalized;
            float dist = Vector2.Distance(transform.parent.position, hit.transform.position);

            // Úhel
            if (Vector2.Angle(facingDir, dirToEnemy) < attackAngle / 2f)
            {
                // Zeï (Raycast)
                if (!Physics2D.Raycast(transform.parent.position, dirToEnemy, dist, obstacleLayers))
                {
                    // Zranìní
                    EnemyStats eStats = hit.GetComponent<EnemyStats>();
                    if (eStats != null)
                    {
                        eStats.TakeDamage(finalDamage);
                    }
                }
            }
        }
    }

    void UpdateAttackPointPosition(Animator anim)
    {
        if (anim != null)
        {
            float x = anim.GetFloat("LastHorizontal");
            float y = anim.GetFloat("LastVertical");
            // Posuneme tento objekt (SwordHolder) trochu ve smìru pohledu pro lepší debug
            transform.localPosition = new Vector3(x, y, 0).normalized * 0.5f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (transform.parent != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}