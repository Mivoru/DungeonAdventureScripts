using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // <--- DÙLEŽITÉ: Pøidat pro práci s textem

public class PlayerStats : CharacterStats
{
    [Header("Level System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int requiredXP = 100;

    [Header("Growth")]
    public float healthGrowthMultiplier = 1.1f;
    public int damageGrowthAmount = 2;

    [Header("XP UI")] // Nová sekce pro UI
    public HealthBar xpBar;      // Odkaz na skript na XP baru
    public TMP_Text levelText;   // Odkaz na text s èíslem levelu

    public override void Start()
    {
        base.Start(); // Nastaví HP a Health Bar

        // --- AUTOMATICKÉ HLEDÁNÍ UI (Aby se to propojilo samo) ---

        // 1. Najdeme XP Bar
        if (xpBar == null)
        {
            GameObject xpObj = GameObject.Find("XPBarBG");
            if (xpObj != null) xpBar = xpObj.GetComponent<HealthBar>();
        }

        // 2. Najdeme Level Text
        if (levelText == null)
        {
            GameObject textObj = GameObject.Find("LevelText");
            if (textObj != null) levelText = textObj.GetComponent<TMP_Text>();
        }
        // ---------------------------------------------------------

        UpdateLevelUI(); // Inicializace zobrazení na startu
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        // Kontrola Level Upu (cyklus while pro pøípad, že dostaneš mega moc XP)
        while (currentXP >= requiredXP)
        {
            LevelUp();
        }

        UpdateLevelUI(); // Aktualizace po pøidání XP
    }

    void LevelUp()
    {
        currentLevel++;
        currentXP -= requiredXP;

        requiredXP = Mathf.RoundToInt(requiredXP * 1.2f);

        maxHealth = Mathf.RoundToInt(maxHealth * healthGrowthMultiplier);
        currentHealth = maxHealth;
        baseDamage += damageGrowthAmount;

        Debug.Log("LEVEL UP!");

        // Aktualizace HP Baru (protože se zmìnilo Max HP a uzdravili jsme se)
        UpdateHealthUI(); // Metoda z rodièe (CharacterStats)
    }

    // Nová pomocná metoda pro aktualizaci XP a Levelu
    void UpdateLevelUI()
    {
        // Aktualizace Baru (používáme stejnou logiku jako u HP: aktuální / maximum)
        if (xpBar != null)
        {
            xpBar.UpdateBar(currentXP, requiredXP);
        }

        // Aktualizace Textu
        if (levelText != null)
        {
            levelText.text = "Lvl " + currentLevel;
        }
    }

    public override void Die()
    {
        base.Die();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public int GetTotalDamage(int weaponBaseDamage = 0, float weaponMultiplier = 1f)
    {
        return Mathf.RoundToInt((baseDamage + weaponBaseDamage) * weaponMultiplier);
    }
}