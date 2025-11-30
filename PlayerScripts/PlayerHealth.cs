using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private HealthBar healthBar; // Už to není public, najdeme si to sami

    void Start()
    {
        currentHealth = maxHealth;

        // --- AUTOMATICKÉ HLEDÁNÍ UI ---
        // Hledáme objekt v scénì, který se jmenuje PØESNÌ "PlayerHealthUI"
        GameObject uiObj = GameObject.Find("PlayerHealthUI");
        if (uiObj != null)
        {
            healthBar = uiObj.GetComponent<HealthBar>();
            healthBar.UpdateBar(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerHealth: Nemùžu najít objekt 'PlayerHealthUI' v Canvasu!");
        }
        // -----------------------------
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);

        if (currentHealth <= 0) Die();
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    void Die()
    {
        Debug.Log("Player Zemøel!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}