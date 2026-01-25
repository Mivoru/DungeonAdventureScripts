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

    [Header("Economy")]
    public int currentCoins = 100;
    [Header("UI Settings")]
    public Vector3 damageOffset = new Vector3(0, 1.0f, 0); // Pozice pro èervená èísla
    public Vector3 healOffset = new Vector3(0, 1.5f, 0);   // Pozice pro zelená èísla

    [Header("Status Effects")]
    private bool isPoisoned = false;
    private Coroutine poisonCoroutine;
    private SpriteRenderer sr;
    private Color originalColor = Color.white;

    [Header("Movement Reference")]
    public PlayerMovement movementScript; // PØETÁHNI SEM HRÁÈE V INSPECTORU!
    //private float originalSpeed;
    private Coroutine slowCoroutine;


    // --- SNAPSHOT DATA (Pro návrat po smrti) ---
    private int savedLevel;
    private int savedXP;
    private int savedRequiredXP;
    private int savedStatPoints;
    private int savedMaxHealth;
    private int savedBaseDamage;
    private int savedCoins;

    // Snapshoty nových statù
    private int savedDefense;
    private float savedCritChance;
    private float savedCritDamage;
    private float savedAttackSpeed;
    private float savedDashCooldownRed;
    private float savedLuck;
    private float savedRegeneration;

    private GameObject activeDeathScreen;
    private bool isDead = false;

    // --- 1. AWAKE: Singleton a DontDestroyOnLoad ---
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Znièit duplikát
            return;
        }
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
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

        // Pokud jsme ve Vesnici, resetujeme vše
        if (scene.name == "VillageScene")
        {
            isDead = false; // OŽIVENÍ (Reset pojistky)
            transform.position = Vector3.zero;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    public override void Start()
    {
        base.Start();
        //if (movementScript != null) originalSpeed = movementScript.moveSpeed;
        FindUIElements();
        UpdateLevelUI();

        // Spustit regeneraci
        StartCoroutine(RegenerationRoutine());
    }
    public void ApplyPoison(float duration, int damagePerTick)
    {
        if (isPoisoned) StopCoroutine(poisonCoroutine);
        poisonCoroutine = StartCoroutine(PoisonRoutine(duration, damagePerTick));
    }

    IEnumerator PoisonRoutine(float duration, int damage)
    {
        isPoisoned = true;
        // Zmìna barvy na zelenou
        if (sr != null) sr.color = Color.green;
        Debug.Log("Jsem otráven!");

        float timer = 0;
        while (timer < duration)
        {
            yield return new WaitForSeconds(1f);

            // Voláme tvoji existující metodu pro poškození
            TakeDamage(damage);
            Debug.Log($"Jed ubral {damage} HP.");

            timer++;
        }

        // Návrat barvy
        if (sr != null) sr.color = originalColor;
        isPoisoned = false;
    }
    public void ApplySlowness(float slowFactor, float duration)
    {
        if (movementScript == null) return;

        // Pokud už je zpomalený, resetujeme timer (zastavíme starou coroutinu)
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowRoutine(slowFactor, duration));
    }

    IEnumerator SlowRoutine(float factor, float duration)
    {
        // OPRAVA: Místo 'moveSpeed' mìníme 'slowMultiplier'
        if (movementScript != null)
        {
            movementScript.slowMultiplier = factor;
        }

        // Debug.Log("Hráè zpomalen!");

        yield return new WaitForSeconds(duration);

        // Návrat na plnou rychlost
        if (movementScript != null)
        {
            movementScript.slowMultiplier = 1f;
        }

        slowCoroutine = null;
        // Debug.Log("Rychlost obnovena.");
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
        savedCoins = currentCoins;

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
        currentCoins = savedCoins;

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
            yield return new WaitForSeconds(1f); // Jednou za sekundu

            if (!isDead && currentHealth < maxHealth && currentHealth > 0 && regeneration > 0)
            {
                // VZOREC: (MaxHP * Procenta) / 100
                // Pøíklad: Máš 100 HP, regen je 1.0 (znamená 1%).
                // 100 * 1 / 100 = 1 HP.
                // Pokud máš 1000 HP -> 10 HP.

                float amountToHeal = (maxHealth * regeneration) / 100f;

                // Musíme ošetøit, aby to healovalo aspoò 1, pokud je výsledek malý (napø. 0.5)
                int finalHeal = Mathf.Max(1, Mathf.RoundToInt(amountToHeal));

                // Voláme Heal, ale s parametrem 'isRegen = true', abychom mohli vypnout text, kdyby to spamovalo
                Heal(finalHeal);
            }
        }
    }

    // --- 4. LEVEL UP SYSTEM ---
    public void AddXP(int amount)
    {
        currentXP += amount;
        while (currentXP >= requiredXP)
        {
            AudioManager.instance.PlaySFX("LevelUp");
            LevelUp();
        }
        UpdateLevelUI();
    }

    void LevelUp()
    {
        currentLevel++;
        currentXP -= requiredXP;
        // Ztížení levelování (zmìò 1.2f na 1.3f pokud chceš hardcore)
        requiredXP = Mathf.RoundToInt(requiredXP * 1.5f);

        // --- ZVÝŠENÍ ODMÌNY ZA LEVEL ---

        // 1. HP: Místo násobení (což dìlá obrovská èísla pozdìji) pøièti pevnou hodnotu + bonus
        // Pùvodnì: maxHealth * 1.1f;
        // Novì: +10 HP fixnì + 5 % navíc
        int healthIncrease = 10 + Mathf.RoundToInt(maxHealth * 0.05f);
        maxHealth += healthIncrease;
        currentHealth = maxHealth;

        // 2. Damage: Pøidej víc damage
        // Pùvodnì: baseDamage += 2;
        baseDamage += 3; // Zkus dát 3 nebo 4, to je na zaèátku hodnì znát

        // 3. Stat Body
        statPoints += 2;

        Debug.Log($"LEVEL UP! Level {currentLevel}. Body: {statPoints}");
        UpdateHealthUI();
        UpdateLevelUI();

        if (levelUpUI != null) levelUpUI.UpdateUI();
    }

    public void UpdateLevelUI()
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
            case "CritChance": critChance += 0.5f; break;
            case "CritDamage": critDamage += 0.05f; break;
            case "AttackSpeed": attackSpeed += 0.01f; break;
            case "Dash": dashCooldownRed += 0.025f; break;
            case "Luck": luck += 0.1f; break;
            case "Regen": regeneration += 0.1f; break;
        }

        statPoints--;
        if (levelUpUI != null) levelUpUI.UpdateUI();
    }

    // --- 5. BOJ & OBRANA ---

    // Pøepisujeme TakeDamage, abychom zapoèítali Defense
    public override void TakeDamage(int damage, bool isCrit = false)
    {
        if (isDead) return;
        int finalDamage = Mathf.Max(1, damage - defense);

        // --- Plovoucí text ---
        if (FloatingTextManager.instance != null)
        {
            FloatingTextManager.instance.ShowDamage(finalDamage, transform.position + damageOffset, isCrit);
        }

        // --- Volání rodièe (odeètení HP) ---
        base.TakeDamage(finalDamage, isCrit);

        // --- Zvuk a Animace ---
        AudioManager.instance.PlaySFX("PlayerHit");

        if (currentHealth > 0)
        {
            Animator anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Hit");

            // --- NOVÉ: Spustíme èervené probliknutí ---
            StartCoroutine(DamageFlash());
        }
    }

    // --- NOVÁ COROUTINA PRO BLIKÁNÍ ---
    IEnumerator DamageFlash()
    {
        // 1. Okamžitì zèervenáme (èistì èervená barva)
        if (sr != null) sr.color = Color.red;

        // 2. Poèkáme malou chvilku (0.1 sekundy je standard pro "hit")
        yield return new WaitForSeconds(0.1f);

        // 3. Návrat barvy (Chytrá kontrola)
        if (sr != null)
        {
            if (isPoisoned)
            {
                // Pokud jsme stále otrávení, vrátíme se k zelené
                sr.color = Color.green;
            }
            else
            {
                // Jinak se vrátíme k normálu (originalColor si ukládáš ve Startu)
                sr.color = originalColor;
            }
        }
    }

    // Zmìna: používáme 'out bool isCrit' abychom vrátili dvì hodnoty
    public int GetCalculatedDamage(int weaponDamage, out bool isCrit)
    {
        int totalBase = baseDamage + weaponDamage;
        isCrit = false;

        if (UnityEngine.Random.Range(0f, 100f) <= critChance)
        {
            isCrit = true;
            return Mathf.RoundToInt(totalBase * critDamage);
        }
        return totalBase;
    }

    // --- 6. SMRT A NÁVRAT ---

    public override void Die()
    {
        // POJISTKA: Pokud už probíhá smrt, nic nedìlej a odejdi
        if (isDead) return;
        AudioManager.instance.PlaySFX("PlayerDeath");
        isDead = true; // Zvedneme vlajku "Jsem mrtvý"
        Debug.Log("Hráè zemøel (Poprvé)!");

        // 1. Zobrazit Death Screen (pokud už není zobrazená)
        if (deathScreenPrefab != null && activeDeathScreen == null)
        {
            GameObject canvas = GameObject.Find("HUD");
            if (canvas)
            {
                activeDeathScreen = Instantiate(deathScreenPrefab, canvas.transform);
                activeDeathScreen.transform.SetAsLastSibling();
                AudioManager.instance.PlaySFX("PlayerDeath");

            }
        }

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Die");

        GetComponent<PlayerMovement>().enabled = false;
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pi != null) pi.enabled = false;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        StartCoroutine(ReturnToVillageRoutine());
    }

    IEnumerator DeathSequence() // Nebo ReturnToVillageRoutine
    {
        // 1. Èekáme na animaci
        yield return new WaitForSecondsRealtime(2f);

        // 2. Znièíme Death Screen
        if (activeDeathScreen != null) Destroy(activeDeathScreen);

        Time.timeScale = 1f;

        // 3. Penalizace
        if (GameManager.instance != null) GameManager.instance.HandleDeathPenalty();

        // 4. Naètení scény
        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");

        // 5. Reset Hráèe
        currentHealth = maxHealth;
        transform.position = Vector3.zero;

        // 6. ZAPNUTÍ OVLÁDÁNÍ (TOHLE JE TA OPRAVA)
        GetComponent<PlayerMovement>().enabled = true;
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (pi != null)
        {
            pi.enabled = true; // Zapneme komponentu

            // 2. VYNUCENÍ MAPY OVLÁDÁNÍ (Kritické!)
            // Bez toho mùže Input System zùstat v "limbu" nebo v jiné mapì (napø. UI)
            if (pi.actions != null)
            {
                var playerMap = pi.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    playerMap.Enable(); // Aktivujeme mapu
                }

                pi.SwitchCurrentActionMap("Player"); // Pøepneme na ni
            }
        }

        // 7. Reset Animátoru
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 8. Reset UI
        FindUIElements();
        UpdateHealthUI();
        UpdateLevelUI();
    }


    System.Collections.IEnumerator ReturnToVillageRoutine()
    {
        yield return new WaitForSecondsRealtime(2f);

        // 2. Znièíme Death Screen
        if (activeDeathScreen != null) Destroy(activeDeathScreen);

        Time.timeScale = 1f;

        // 3. Penalizace
        if (GameManager.instance != null) GameManager.instance.HandleDeathPenalty();

        // 4. Naètení scény
        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");

        // 5. Reset Hráèe
        currentHealth = maxHealth;
        transform.position = Vector3.zero;

        // 6. ZAPNUTÍ OVLÁDÁNÍ (TOHLE JE TA OPRAVA)
        GetComponent<PlayerMovement>().enabled = true;
        var pi = GetComponent<UnityEngine.InputSystem.PlayerInput>();

        if (pi != null)
        {
            pi.enabled = true; // Zapneme komponentu

            // 2. VYNUCENÍ MAPY OVLÁDÁNÍ (Kritické!)
            // Bez toho mùže Input System zùstat v "limbu" nebo v jiné mapì (napø. UI)
            if (pi.actions != null)
            {
                var playerMap = pi.actions.FindActionMap("Player");
                if (playerMap != null)
                {
                    playerMap.Enable(); // Aktivujeme mapu
                }

                pi.SwitchCurrentActionMap("Player"); // Pøepneme na ni
            }
        }

        // 7. Reset Animátoru
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // 8. Reset UI
        FindUIElements();
        UpdateHealthUI();
        UpdateLevelUI();
    }
    // DÙLEŽITÉ: Musí zde být 'bool', nikoliv 'void'
    public void Heal(int amount)
    {
        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        UpdateHealthUI();

        if (FloatingTextManager.instance != null)
        {
            // ZDE POUŽIJEME healOffset
            FloatingTextManager.instance.ShowHeal(amount, transform.position + healOffset);
        }
    }
    public void RecalculateRequiredXP()
    {
        // Resetujeme na základ
        requiredXP = 150; // Tvoje startovní hodnota

        // Nasimulujeme rùst køivky až do aktuálního levelu
        for (int i = 1; i < currentLevel; i++)
        {
            requiredXP = Mathf.RoundToInt(requiredXP * 1.5f); // Tvùj násobiè (1.5f)
        }

        Debug.Log($"Level {currentLevel} naèten. Pøepoèítané requiredXP: {requiredXP}");
        UpdateLevelUI();
    }
}