using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyStats : CharacterStats
{
    [Header("Enemy Info")]
    public string enemyName = "Enemy";

    [Header("Enemy Level")]
    public int level = 1;

    [Header("Scaling Settings")]
    public int healthPerLevel = 10;
    public int damagePerLevel = 3;
    public int xpRewardPerLevel = 5;

    [Header("Rewards")]
    public int baseXpReward = 20;

    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0, 100)] public float dropChance;
    }

    [Header("Loot Settings")]
    public GameObject lootPrefab;
    public List<LootEntry> lootTable;

    [Header("Drop Scatter")]
    public float scatterRadius = 1.0f;

    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text nameText;

    // Interní promìnné pro bezpeèné škálování
    private int _startMaxHealth;
    private int _startBaseDamage;
    private bool _initialized = false;

    public EnemyData data;

    void Awake()
    {
        // Pokud máme pøiøazená data, naèteme je
        if (data != null)
        {
            enemyName = data.enemyName;
            maxHealth = data.maxHealth; // Naèteme základ
            baseDamage = data.baseDamage;
            lootTable = data.lootTable;
            lootPrefab = data.lootPrefab;

            // Uložíme pro levelování
            _startMaxHealth = maxHealth;
            _startBaseDamage = baseDamage;
        }
    }
    // ... zbytek skriptu (Start, Lev

    public override void Start()
    {
        // Pokud nebyl level nastaven externì, inicializujeme teï
        if (!_initialized)
        {
            ApplyLevelStats();
        }

        // --- AUTOMATICKÉ HLEDÁNÍ UI ---
        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();

        if (levelText == null)
        {
            Transform lvlObj = transform.Find("HealthCanvas/LevelText");
            if (lvlObj != null) levelText = lvlObj.GetComponent<TMP_Text>();
        }

        if (nameText == null)
        {
            Transform nameObj = transform.Find("HealthCanvas/NameText");
            if (nameObj != null) nameText = nameObj.GetComponent<TMP_Text>();
        }
        // -----------------------------

        base.Start(); // Zavolá Start rodièe (nastaví currentHealth)
        UpdateUI();
    }

    // Voláno z Generátoru
    public void SetLevel(int newLevel)
    {
        level = newLevel;
        ApplyLevelStats();
    }

    void ApplyLevelStats()
    {
        // 1. ZÁCHRANA PROTI NULÁM
        // Pokud v Inspectoru nìkdo nechal 0, nastavíme základní hodnoty.
        if (_startMaxHealth <= 0)
        {
            _startMaxHealth = 50; // Default HP
            // Pokud jsme to zjistili až teï, aktualizujeme i pùvodní promìnnou
            maxHealth = 50;
        }
        if (_startBaseDamage <= 0)
        {
            _startBaseDamage = 5; // Default Damage
            baseDamage = 5;
        }

        // 2. VÝPOÈET STATÙ PODLE LEVELU
        if (level > 1)
        {
            maxHealth = _startMaxHealth + ((level - 1) * healthPerLevel);
            baseDamage = _startBaseDamage + ((level - 1) * damagePerLevel);
        }
        else
        {
            // Pro Level 1 použijeme èistý základ
            maxHealth = _startMaxHealth;
            baseDamage = _startBaseDamage;
        }

        // 3. INICIALIZACE ŽIVOTA
        currentHealth = maxHealth;
        _initialized = true;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (levelText != null) levelText.text = "Lvl " + level;
        if (nameText != null) nameText.text = enemyName;
        if (healthBar != null) healthBar.UpdateBar(currentHealth, maxHealth);
    }

    public override void TakeDamage(int damage)
    {
        // 1. KONTROLA ARCHERA (EVASION)
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();

        if (archerAI != null && archerAI.IsEvading())
        {
            // --- NOVÝ VÝPIS: ÚSPÌŠNÝ ÚHYB ---
            Debug.Log("<color=green> ARCHER: Støela mì minula! (Jsem v úhybu -> 0 Damage)</color>");
            return;
        }

        // 2. KONTROLA WARRIORA (BLOCK - Štít)
        // Nejdøív musíme získat Animátor
        Animator anim = GetComponent<Animator>();

        // A musíme zjistit, jestli je to Warrior. 
        // Pokud bychom se zeptali Archera na "IsBlocking", hodil by Error, protože ten parametr nemá.
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();

        if (warriorAI != null && anim != null)
        {
            // Ptáme se na blokování jen pokud je to Warrior
            if (anim.GetBool("IsBlocking"))
            {
                return; // Zblokováno štítem, ignorujeme poškození
            }
        }

        // 3. UDÌLENÍ POŠKOZENÍ (Základní logika - odeètení HP)
        base.TakeDamage(damage);

        // 4. SPUŠTÌNÍ ANIMACE "HIT"
        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }

        // 5. AGGRO (Upozornìní AI, že jsme pod útokem)
        // Probudíme jakýkoliv typ AI, který na tomto objektu je
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.TriggerAggro();

        if (warriorAI != null) warriorAI.TriggerAggro();

        if (archerAI != null) archerAI.TriggerAggro();

        // Specifické pro Slima (ten má vlastní metodu pro Hurt animaci)
        SlimeAI slime = GetComponent<SlimeAI>();
        if (slime != null) slime.TriggerHurtAnim();

        // 6. OMRÁÈENÍ (STUN)
        // Zastavíme AI na chvíli, aby se pøehrála animace bolesti a neutíkal dál
        StartCoroutine(StunRoutine());
    }

    // --- POMOCNÁ COROUTINA PRO OMRÁÈENÍ ---
    System.Collections.IEnumerator StunRoutine()
    {
        // Získáme reference na možné AI skripty na tomto objektu
        EnemyAI ai = GetComponent<EnemyAI>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        SlimeAI slimeAI = GetComponent<SlimeAI>();

        // VYPNOUT AI (Zastaví logiku pohybu a útoèení)
        if (ai != null) ai.enabled = false;
        if (warriorAI != null) warriorAI.enabled = false;
        if (archerAI != null) archerAI.enabled = false;
        if (slimeAI != null) slimeAI.enabled = false;

        // Èekáme délku animace Hurt (cca 0.4 sekundy)
        yield return new WaitForSeconds(0.4f);

        // ZAPNOUT AI (Vrátí se k normálnímu chování)
        if (ai != null) ai.enabled = true;
        if (warriorAI != null) warriorAI.enabled = true;
        if (archerAI != null) archerAI.enabled = true;
        if (slimeAI != null) slimeAI.enabled = true;
    }


    public override void Die()
    {
        base.Die();

        // XP pro hráèe
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            int finalXp = baseXpReward + ((level - 1) * xpRewardPerLevel);
            player.GetComponent<PlayerStats>()?.AddXP(finalXp);
        }

        DropLoot();

        // Znièení objektu
        Destroy(gameObject);
    }

    void DropLoot()
    {
        if (lootPrefab == null || lootTable == null) return;

        foreach (LootEntry entry in lootTable)
        {
            float roll = UnityEngine.Random.Range(0f, 100f);

            if (roll <= entry.dropChance)
            {
                Vector3 scatterOffset = (Vector3)UnityEngine.Random.insideUnitCircle * scatterRadius;
                Vector3 spawnPos = transform.position + scatterOffset;

                GameObject loot = Instantiate(lootPrefab, spawnPos, Quaternion.identity);

                LootPickup pickupScript = loot.GetComponent<LootPickup>();
                if (pickupScript != null)
                {
                    pickupScript.SetItem(entry.item);
                }
            }
        }
    }
}