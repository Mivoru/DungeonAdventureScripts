using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 50;
    private int currentHealth;

    // Reference to the HealthBar script local to this enemy
    public HealthBar healthBar;

    void Start()
    {
        currentHealth = maxHealth;

        // Find the health bar component in children if not assigned manually
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>();

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Add death effects here (particles, loot)
        Destroy(gameObject);
    }
}