using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // <--- 1. PØIDÁNO: Nutné pro detekci UI

public class SwordAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackAngle = 100f;

    [Header("Timing")]
    public float baseAttackDuration = 0.3f;
    public float attackCooldown = 0.5f;

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
        // --- 2. PØIDÁNO: OCHRANA PROTI KLIKNUTÍ DO UI ---
        // Pokud myš stojí na tlaèítku nebo inventáøi, okamžitì skonèíme.
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }
        // -----------------------------------------------

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
            playerAnim.SetFloat("AttackSpeed", currentAttackSpeed);
            playerAnim.SetTrigger("Attack");

            // Pojistka: Pøehrát zvuk jen pokud existuje AudioManager
            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX("SwordSwing");
            }
        }

        // 3. Aktualizace pozice hitboxu
        UpdateAttackPointPosition(playerAnim);

        // Výpoèet trvání
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
        // A) Výpoèet poškození + Crit
        PlayerStats stats = GetComponentInParent<PlayerStats>();

        int finalDamage = weaponDamage;
        bool isCrit = false;

        if (stats != null)
        {
            finalDamage = stats.GetCalculatedDamage(weaponDamage, out isCrit);
        }

        // B) Zjištìní smìru
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
                        eStats.TakeDamage(finalDamage, isCrit);
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