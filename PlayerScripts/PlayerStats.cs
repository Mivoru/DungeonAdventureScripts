using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlayerStats : CharacterStats
{
    public static PlayerStats instance; // Singleton

    [Header("Level System")]
    public int currentLevel = 1;
    public int currentXP = 0;
    public int requiredXP = 100;
    public int statPoints = 0; // Body na vylepšování

    [Header("Growth (Automatické)")]
    public float healthGrowthMultiplier = 1.1f;
    public int damageGrowthAmount = 2;

    [Header("Custom Stats (Vylepšitelné)")]
    public int defense = 0;          // Snižuje poškození
    public float critChance = 5f;    // Šance v %
    public float critDamage = 1.5f;  // Násobiè
    public float attackSpeed = 1.0f; // Rychlost útoku
    public float dashCooldownRed = 0f; // Zkrácení cooldowu
    public float luck = 1.0f;        // Drop rate
    public float regeneration = 0f;  // HP za sekundu

    [Header("UI References")]
    public HealthBar xpBar;
    public TMP_Text levelText;
    public LevelUpUI levelUpUI; // Odkaz na okno levelování

    [Header("Death UI")]
    public GameObject deathScreenPrefab;

    // --- SNAPSHOT DATA (Pro návrat po smrti) ---
    private int savedLevel;
    private int savedXP;
    private int savedRequiredXP;
    private int savedStatPoints;
    private int savedMaxHealth;
    private int savedBaseDamage;

    // Snapshoty nových statù
    private int savedDefense;
    private float savedCritChance;
    private float savedCritDamage;
    private float savedAttackSpeed;
    private float savedDashCooldownRed;
    private float savedLuck;
    private float savedRegeneration;

    private GameObject activeDeathScreen;

    // --- 1. AWAKE: Singleton a DontDestroyOnLoad ---
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Znièit duplikát
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // Pøežít naèítání scén

        if (currentHealth <= 0) currentHealth = maxHealth;
    }

    // --- 2. SCENE MANAGEMENT: Hledání UI po naètení ---
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindUIElements();
        UpdateHealthUI();
        UpdateLevelUI();

        // Pokud jsme ve Vesnici, resetujeme pozici na start
        if (scene.name == "VillageScene")
        {
            transform.position = Vector3.zero;
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public override void Start()
    {
        base.Start();
        FindUIElements();
        UpdateLevelUI();

        // Spustit regeneraci
        StartCoroutine(RegenerationRoutine());
    }

    // --- SNAPSHOT SYSTÉM ---
    // Zavolá GameManager pøed vstupem do Dungeonu
    public void SaveSnapshot()
    {
        savedLevel = currentLevel;
        savedXP = currentXP;
        savedRequiredXP = requiredXP;
        savedStatPoints = statPoints;
        savedMaxHealth = maxHealth;
        savedBaseDamage = baseDamage;

        // Uložení nových statù
        savedDefense = defense;
        savedCritChance = critChance;
        savedCritDamage = critDamage;
        savedAttackSpeed = attackSpeed;
        savedDashCooldownRed = dashCooldownRed;
        savedLuck = luck;
        savedRegeneration = regeneration;

        Debug.Log("Player Stats Snapshot ULOŽEN.");
    }

    // Zavolá GameManager po smrti (Reset na stav pøed dungeonem)
    public void LoadSnapshot()
    {
        currentLevel = savedLevel;
        currentXP = savedXP;
        requiredXP = savedRequiredXP;
        statPoints = savedStatPoints;
        maxHealth = savedMaxHealth;
        baseDamage = savedBaseDamage;

        // Obnovení nových statù
        defense = savedDefense;
        critChance = savedCritChance;
        critDamage = savedCritDamage;
        attackSpeed = savedAttackSpeed;
        dashCooldownRed = savedDashCooldownRed;
        luck = savedLuck;
        regeneration = savedRegeneration;

        currentHealth = maxHealth; // Vyléèit po resetu
        UpdateLevelUI();
        UpdateHealthUI();
        Debug.Log("Player Stats Snapshot NAÈTEN (Reset).");
    }

    void FindUIElements()
    {
        if (xpBar == null)
        {
            GameObject xpObj = GameObject.Find("XPBarBG");
            if (xpObj != null) xpBar = xpObj.GetComponent<HealthBar>();
        }
        if (levelText == null)
        {
            GameObject txtObj = GameObject.Find("LevelText");
            if (txtObj != null) levelText = txtObj.GetComponent<TMP_Text>();
        }
        if (healthBar == null)
        {
            GameObject hpObj = GameObject.Find("PlayerHealthUI");
            if (hpObj != null) healthBar = hpObj.GetComponent<HealthBar>();
        }

        // Najít LevelUpUI (pokud existuje ve scénì)
        if (levelUpUI == null)
        {
            levelUpUI = FindFirstObjectByType<LevelUpUI>();
        }
    }

    // --- 3. REGENERACE ---
    IEnumerator RegenerationRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (currentHealth < maxHealth && currentHealth > 0 && regeneration > 0)
            {
                Heal(Mathf.RoundToInt(regeneration));
            }
        }
    }

    // --- 4. LEVEL UP SYSTEM ---
    public void AddXP(int amount)
    {
        currentXP += amount;
        while (currentXP >= requiredXP)
        {
            LevelUp();
        }
        UpdateLevelUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentXP -= requiredXP;
        requiredXP = Mathf.RoundToInt(requiredXP * 1.2f);

        // Auto-vylepšení
        maxHealth = Mathf.RoundToInt(maxHealth * healthGrowthMultiplier);
        currentHealth = maxHealth;
        baseDamage += damageGrowthAmount;

        // Body pro hráèe
        statPoints += 2;

        Debug.Log($"LEVEL UP! Level {currentLevel}. Body: {statPoints}");
        UpdateHealthUI();
        UpdateLevelUI();

        if (levelUpUI != null) levelUpUI.UpdateUI();
    }

    void UpdateLevelUI()
    {
        if (xpBar != null) xpBar.UpdateBar(currentXP, requiredXP);
        if (levelText != null) levelText.text = "Lvl " + currentLevel;
    }

    // Metoda pro tlaèítka v UI
    public void UpgradeStat(string statName)
    {
        if (statPoints <= 0) return;

        switch (statName)
        {
            case "Defense": defense += 1; break;
            case "CritChance": critChance += 2f; break;
            case "CritDamage": critDamage += 0.1f; break;
            case "AttackSpeed": attackSpeed += 0.05f; break;
            case "Dash": dashCooldownRed += 0.1f; break;
            case "Luck": luck += 0.2f; break;
            case "Regen": regeneration += 1f; break;
        }

        statPoints--;
        if (levelUpUI != null) levelUpUI.UpdateUI();
    }

    // --- 5. BOJ & OBRANA ---

    // Pøepisujeme TakeDamage, abychom zapoèítali Defense
    public override void TakeDamage(int damage)
    {
        int finalDamage = Mathf.Max(1, damage - defense); // Defense snižuje dmg

        base.TakeDamage(finalDamage);

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Hit");
    }

    // Výpoèet útoku (Crit) - volá meè/luk
    public int GetCalculatedDamage(int weaponDamage)
    {
        int totalBase = baseDamage + weaponDamage;

        if (UnityEngine.Random.Range(0f, 100f) <= critChance)
        {
            // Debug.Log("CRITICAL HIT!");
            return Mathf.RoundToInt(totalBase * critDamage);
        }
        return totalBase;
    }

    // --- 6. SMRT A NÁVRAT ---

    public override void Die()
    {
        Debug.Log("Hráè zemøel!");

        // 1. Zobrazit Death Screen a ULOŽIT SI HO
        if (deathScreenPrefab != null)
        {
            GameObject canvas = GameObject.Find("HUD");
            if (canvas)
            {
                activeDeathScreen = Instantiate(deathScreenPrefab, canvas.transform);
                // Zajistíme, že bude navrchu (pøekryje vše)
                activeDeathScreen.transform.SetAsLastSibling();
            }
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        GetComponent<PlayerMovement>().enabled = false;
        GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Èekáme 2 vteøiny (jak jsi chtìl)
        yield return new WaitForSecondsRealtime(2f);

        // ZNIÈÍME DEATH SCREEN PØED NAÈTENÍM VESNICE
        if (activeDeathScreen != null)
        {
            Destroy(activeDeathScreen);
        }

        Time.timeScale = 1f;

        if (GameManager.instance != null)
        {
            GameManager.instance.HandleDeathPenalty();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");

        currentHealth = maxHealth;
        transform.position = Vector3.zero;

        GetComponent<PlayerMovement>().enabled = true;
        GetComponent<UnityEngine.InputSystem.PlayerInput>().enabled = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }


IEnumerator ReturnToVillageRoutine()
    {
        yield return new WaitForSecondsRealtime(3f);
        Time.timeScale = 1f;

        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");

        // OnSceneLoaded se postará o reset pozice a UI
        currentHealth = maxHealth;

        GetComponent<PlayerMovement>().enabled = true;
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = true;

        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}