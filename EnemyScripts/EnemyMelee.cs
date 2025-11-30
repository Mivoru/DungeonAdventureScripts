using System.Collections;
using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    public float attackRate = 1.5f;
    public int damage = 15;
    public float attackRange = 1.5f;
    public float attackAngle = 90f;

    public LayerMask playerLayer; // Vrstva "Player"
    public Transform swordVisual; // Grafika meèe pro animaci

    private float nextAttackTime;
    private EnemyAI ai;
    private bool isAttacking = false;

    void Start()
    {
        ai = GetComponent<EnemyAI>();
    }

    void Update()
    {
        if (ai != null && ai.IsInAttackRange && !isAttacking)
        {
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
                nextAttackTime = Time.time + attackRate;
            }
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. Zásah (Výseè)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange, playerLayer);
        foreach (var hit in hits)
        {
            Vector2 dirToPlayer = (hit.transform.position - transform.position).normalized;
            if (Vector2.Angle(transform.up, dirToPlayer) < attackAngle / 2f)
            {
                // Zpùsobíme poškození hráèi
                // Zde používáme PlayerStats místo EnemyStats!
                hit.GetComponent<PlayerStats>()?.TakeDamage(damage);
            }
        }

        // 2. Animace meèe (stejná logika jako u hráèe)
        if (swordVisual != null)
        {
            float timer = 0;
            Quaternion start = Quaternion.Euler(0, 0, 45);
            Quaternion end = Quaternion.Euler(0, 0, -45);
            Quaternion initialRot = swordVisual.localRotation;

            while (timer < 0.2f)
            {
                swordVisual.localRotation = Quaternion.Lerp(initialRot * start, initialRot * end, timer / 0.2f);
                timer += Time.deltaTime;
                yield return null;
            }
            swordVisual.localRotation = initialRot;
        }

        yield return new WaitForSeconds(0.5f); // Pauza po útoku
        isAttacking = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}