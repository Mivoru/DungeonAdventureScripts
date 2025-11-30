using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwordAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 2.5f;
    public float attackAngle = 120f;
    public float attackDuration = 0.2f;

    [Header("Weapon Stats")]
    public int weaponDamage = 10;       // Základní poškození meèe
    public float damageMultiplier = 1f; // Pøípadnı bonus (1.0 = 100%)

    [Header("Layers")]
    public LayerMask enemyLayers;
    public LayerMask obstacleLayers;

    private bool isAttacking = false;
    private Quaternion defaultRotation;

    void Start()
    {
        // Uloíme si startovní rotaci SwordHolderu
        defaultRotation = transform.localRotation;
    }

    // Voláno z WeaponManageru
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && !isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        // 1. Zpùsobit poškození
        DealConeDamage();

        // 2. Animace (Otáèíme SwordHolderem)
        float timer = 0f;

        // Vypoèítáme úhly rotace
        Quaternion startRot = Quaternion.Euler(0, 0, attackAngle / 2f);
        Quaternion endRot = Quaternion.Euler(0, 0, -attackAngle / 2f);

        while (timer < attackDuration)
        {
            transform.localRotation = Quaternion.Lerp(defaultRotation * startRot, defaultRotation * endRot, timer / attackDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = defaultRotation;
        isAttacking = false;
    }

    void DealConeDamage()
    {
        // Získáme reference na statistiky hráèe (pro vıpoèet síly)
        // SwordHolder je dítì Player objektu, proto GetComponentInParent
        PlayerStats stats = GetComponentInParent<PlayerStats>();

        // Vıpoèet finálního poškození (Staty hráèe + Staty zbranì)
        int finalDamage = (stats != null) ? stats.GetTotalDamage(weaponDamage, damageMultiplier) : weaponDamage;

        // Poèítáme útok ze støedu HRÁÈE (Rodièe), ne ze støedu meèe.
        Vector3 origin = transform.parent.position;

        // Hledáme v kruhu kolem hráèe
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRange, enemyLayers);

        foreach (Collider2D hit in hits)
        {
            Vector2 directionToEnemy = (hit.transform.position - origin).normalized;

            // Smìr, kterım se dívá hráè (zajišuje, e útoèíme pøed sebe)
            Vector2 playerFacingDirection = transform.parent.up;

            float angleToEnemy = Vector2.Angle(playerFacingDirection, directionToEnemy);

            // Je nepøítel v naší vıseèi?
            if (angleToEnemy < attackAngle / 2f)
            {
                float dist = Vector2.Distance(origin, hit.transform.position);

                // Raycast pro kontrolu zdí (aby nešlo sekat skrz zeï)
                RaycastHit2D ray = Physics2D.Raycast(origin, directionToEnemy, dist, obstacleLayers);

                if (ray.collider == null)
                {
                    // ZMÌNA: Hledáme EnemyStats místo EnemyHealth
                    EnemyStats eStats = hit.GetComponent<EnemyStats>();
                    if (eStats != null)
                    {
                        eStats.TakeDamage(finalDamage);
                    }
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (transform.parent != null)
        {
            Vector3 origin = transform.parent.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, attackRange);
        }
    }
}