using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 50;

    // ZMÌNA: Musí být public, aby si ji mohl pøeèíst Boss AI skript (pro fáze 50%, 25%...)
    public int currentHealth;

    public bool isInvincible = false;

    // Reference to the HealthBar script local to this enemy
    public HealthBar healthBar;

    void Start()
    {
        // Pokud currentHealth ještì nebyl nastaven (napø. pøes SetLevel), nastavíme ho na max
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }

        // Find the health bar component in children if not assigned manually
        if (healthBar == null)
            healthBar = GetComponentInChildren<HealthBar>();

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        // Pokud je nesmrtelný (skok pavouka, zahrabání bosse), ignoruj zásah
        if (isInvincible) return;

        currentHealth -= damage;

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Vypneme AI a pohyb
        if (GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        // Spustíme animaci smrti
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("Die");
            // Poèkáme délku animace (napø. 1 vteøinu) a pak znièíme
            Destroy(gameObject, 1f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}