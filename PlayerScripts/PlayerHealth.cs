using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Status Effects")]
    public bool isPoisoned = false;
    private Coroutine poisonCoroutine;
    private SpriteRenderer sr;
    private Color originalColor;

    private HealthBar healthBar; // Už to není public, najdeme si to sami
    void Awake()
    {
        // ... tvoje inicializace ...

        // Zmìna: Hledáme SpriteRenderer i na potomcích (dìtech)
        sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
        {
            originalColor = sr.color;
        }
        else
        {
            Debug.LogError("PlayerHealth: Nenašel jsem SpriteRenderer! Postava nebude zelenat.");
        }
    }
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
    public void ApplyPoison(float duration, int damagePerTick)
    {
        if (isPoisoned)
        {
            // Pokud už otrávený je, restartujeme èasovaè (prodloužíme otravu)
            StopCoroutine(poisonCoroutine);
        }

        poisonCoroutine = StartCoroutine(PoisonRoutine(duration, damagePerTick));
    }

    IEnumerator PoisonRoutine(float duration, int damage)
    {
        isPoisoned = true;
        Debug.Log("OTRAVA START: Hráè by mìl zezelenat.");

        // Zmìna barvy
        if (sr != null) sr.color = Color.green;

        float timer = 0;
        while (timer < duration)
        {
            yield return new WaitForSeconds(1f);

            // Aplikace damage
            // Pokud máš metodu TakeDamage, která spouští blikání (invincibility),
            // mùže to pøebít zelenou barvu. 
            // Pro teï to necháme takto:
            if (currentHealth > 0)
            {
                currentHealth -= damage;
                // Aktualizuj HealthBar, pokud máš referenci
                // if (healthBar != null) healthBar.SetHealth(currentHealth);
                Debug.Log($"Jed ubrall {damage} HP. Zbývá: {currentHealth}");
            }

            timer++;
        }

        // Návrat barvy
        if (sr != null) sr.color = originalColor;

        isPoisoned = false;
        Debug.Log("OTRAVA KONEC.");
    }
    void Die()
    {
        Debug.Log("Player Zemøel!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}