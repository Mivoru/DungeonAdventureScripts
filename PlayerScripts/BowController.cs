using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BowController : MonoBehaviour
{
    [Header("References")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    [Header("Charge Settings")]
    public float maxChargeTime = 1.0f; // Čas do plného nátahu
    public float maxSpeed = 40f;
    public float minSpeed = 10f;

    [Header("Damage Settings")]
    public int minDamage = 5;   // Damage při rychlém kliku
    public int maxDamage = 30;  // Damage při plném nátahu

    [Header("Cooldown Settings")]
    public float attackCooldown = 0.5f; // Čas mezi výstřely

    private float chargeStartTime;
    private bool isCharging = false;
    private float nextAttackTime = 0f;

    // Voláno z WeaponManageru
    public void OnAttack(InputAction.CallbackContext context)
    {
        // 1. ZAČÁTEK NATAHOVÁNÍ
        if (context.started)
        {
            if (Time.time >= nextAttackTime)
            {
                isCharging = true;
                chargeStartTime = Time.time;
            }
        }

        // 2. VÝSTŘEL
        if (context.canceled && isCharging)
        {
            isCharging = false;
            float holdTime = Time.time - chargeStartTime;

            FireArrow(holdTime);
        }
    }

    void FireArrow(float holdTime)
    {
        // Síla nátahu (0 až 1)
        float chargePower = Mathf.Clamp01(holdTime / maxChargeTime);

        // --- VÝPOČET POŠKOZENÍ ---
        // Získáme sílu hráče
        PlayerStats stats = GetComponentInParent<PlayerStats>();
        int playerBaseDmg = (stats != null) ? stats.baseDamage : 0;

        // Získáme sílu luku (podle nátahu)
        int bowDamage = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, chargePower));

        // Celkové poškození
        int totalDamage = playerBaseDmg + bowDamage;
        // --------------------------

        // Výpočet rychlosti šípu
        float shootSpeed = Mathf.Lerp(minSpeed, maxSpeed, chargePower);

        // Vytvoření šípu
        GameObject newArrow = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);

        // Nastavení rychlosti
        Rigidbody2D arrowRb = newArrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = firePoint.up * shootSpeed;
        }

        // Předání damage šípu
        ArrowProjectile arrowScript = newArrow.GetComponent<ArrowProjectile>();
        if (arrowScript != null)
        {
            arrowScript.damage = totalDamage;
        }

        // Nastavení cooldownu
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log($"Arrow fired! Power: {chargePower:P0}, Total Dmg: {totalDamage}");
    }
}