using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    [Header("Base Stats")]
    public int maxHealth = 100;
    public int currentHealth;

    [Tooltip("Základní síla útoku (bez zbranì)")]
    public int baseDamage = 10;

    [Tooltip("Rychlost pohybu postavy")]
    public float movementSpeed = 5f; // Zde nastavujeme rychlost pro pohyb i NavMesh

    [Header("UI")]
    public HealthBar healthBar;

    public virtual void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();
    }

    public virtual void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthUI();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public virtual void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        UpdateHealthUI();
    }

    // Protected: Aby to vidìly i dìti (PlayerStats, EnemyStats)
    public void UpdateHealthUI()
    {
        if (healthBar != null)
        {
            healthBar.UpdateBar(currentHealth, maxHealth);
        }
    }

    public virtual void Die()
    {
        Debug.Log($"{transform.name} zemøel.");
    }
}