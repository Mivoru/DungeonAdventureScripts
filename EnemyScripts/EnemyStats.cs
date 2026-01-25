using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 1. Definujeme si druhy nepøátel
public enum EnemyRank
{
    Weak,   // Slime, Krysa (Málo XP)
    Normal, // Skeleton Warrior/Archer (Støední XP)
    Elite,  // Ent, Miniboss (Hodnì XP)
    Boss    // Hlavní boss (Obøí XP)
}

public class EnemyStats : CharacterStats
{
    [Header("Enemy Data")]
    public EnemyData data;

    [Header("Enemy Info")]
    public string enemyName = "Enemy";
    public EnemyRank rank = EnemyRank.Weak;

    [Header("Enemy Level")]
    public int level = 1;

    [Header("Scaling Settings")]
    public int healthPerLevel = 10;
    public int damagePerLevel = 2;

    // --- NOVINKA: Univerzální nesmrtelnost (pro Bosse pøi Burrow, Gianta atd.) ---
    [Header("Status Effects")]
    public bool isInvincible = false;

    [System.Serializable]
    public class LootEntry
    {
        public ItemData item;
        [Range(0, 100)] public float dropChance;
    }

    [Header("Loot Settings")]
    public GameObject lootPrefab;
    public List<LootEntry> lootTable;
    public float scatterRadius = 1.0f;

    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text nameText;

    private int _startMaxHealth;
    private int _startBaseDamage;
    private bool _initialized = false;
    private EnemyAudio enemyAudio;

    void Awake()
    {
        // 1. Pokud máme Data, naèteme je
        if (data != null)
        {
            enemyName = data.enemyName;
            maxHealth = data.maxHealth;
            baseDamage = data.baseDamage;

            if (lootTable == null || lootTable.Count == 0) lootTable = data.lootTable;
            if (lootPrefab == null) lootPrefab = data.lootPrefab;
        }

        // 2. Uložíme startovní hodnoty
        _startMaxHealth = maxHealth;
        _startBaseDamage = baseDamage;

        // 3. Záchranná brzda
        if (_startMaxHealth <= 0) _startMaxHealth = 50;
        if (_startBaseDamage <= 0) _startBaseDamage = 5;
    }

    public override void Start()
    {
        if (!_initialized)
        {
            ApplyLevelStats();
        }

        if (healthBar == null) healthBar = GetComponentInChildren<HealthBar>();

        // Automatické hledání UI textù, pokud nejsou pøiøazeny
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

        base.Start();
        enemyAudio = GetComponent<EnemyAudio>();
        UpdateUI();
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
        ApplyLevelStats();
    }

    void ApplyLevelStats()
    {
        if (level > 1)
        {
            maxHealth = _startMaxHealth + ((level - 1) * healthPerLevel);
            baseDamage = _startBaseDamage + ((level - 1) * damagePerLevel);
        }
        else
        {
            maxHealth = _startMaxHealth;
            baseDamage = _startBaseDamage;
        }

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

    public override void TakeDamage(int damage, bool isCrit = false)
    {
        // 1. KONTROLA NESMRTELNOSTI
        // (Tohle teï funguje pro Gianta i pro Arachne, když je pod zemí)
        GiantAI giant = GetComponent<GiantAI>();
        if (isInvincible || (giant != null && giant.IsInvulnerable()))
        {
            // Debug.Log($"{enemyName} je momentálnì nesmrtelný!");
            return;
        }

        // 2. KONTROLA ÚHYBU (Archer)
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        if (archerAI != null && archerAI.IsEvading()) return;

        // 3. KONTROLA BLOKOVÁNÍ (Warrior)
        Animator anim = GetComponent<Animator>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();

        if (warriorAI != null && anim != null && anim.GetBool("IsBlocking"))
        {
            if (enemyAudio != null) enemyAudio.PlayBlock();
            return;
        }

        // --- APLIKACE POŠKOZENÍ ---

        // Floating Text
        if (FloatingTextManager.instance != null)
        {
            FloatingTextManager.instance.ShowDamage(damage, transform.position, isCrit);
        }

        base.TakeDamage(damage, isCrit); // Ubere HP

        // Zvuk
        if (enemyAudio != null && currentHealth > 0)
        {
            enemyAudio.PlayHurt();
        }

        // Animace zásahu
        if (anim != null) anim.SetTrigger("Hit");

        // --- AGGRO TRIGGER (Reakce AI) ---
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.TriggerAggro();

        if (warriorAI != null) warriorAI.TriggerAggro();
        if (archerAI != null) archerAI.TriggerAggro();

        // Pøidáno pro Bosse (i když ten má aggro asi poøád)
        /* ArachneBossAI bossAI = GetComponent<ArachneBossAI>(); 
           if (bossAI != null) ... */

        SlimeAI slime = GetComponent<SlimeAI>();
        if (slime != null) slime.TriggerHurtAnim();

        // Stun (zastavení na chvíli)
        if (currentHealth > 0) StartCoroutine(StunRoutine());
    }

    public override void Die()
    {
        if (enemyAudio != null)
        {
            enemyAudio.PlayDeath();
        }

        base.Die();

        if (PlayerStats.instance != null)
        {
            int xpAmount = CalculateXP();
            int finalXP = ApplyLevelGapPenalty(xpAmount);
            Debug.Log($"Enemy {rank} (Lvl {level}) killed. XP: {finalXP}");
            PlayerStats.instance.AddXP(finalXP);
        }

        DropLoot();
        Destroy(gameObject);
    }

    System.Collections.IEnumerator StunRoutine()
    {
        // Získáme reference na rùzné AI scripty
        EnemyAI ai = GetComponent<EnemyAI>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        SlimeAI slimeAI = GetComponent<SlimeAI>();
        ArachneBossAI bossAI = GetComponent<ArachneBossAI>(); // PØIDÁNO

        // Vypneme
        if (ai != null) ai.enabled = false;
        if (warriorAI != null) warriorAI.enabled = false;
        if (archerAI != null) archerAI.enabled = false;
        if (slimeAI != null) slimeAI.enabled = false;

        // Bosse vìtšinou nechceme stunovat pøi každém zásahu, 
        // ale pokud ano, odkomentuj toto:
        // if (bossAI != null) bossAI.enabled = false; 

        yield return new WaitForSeconds(0.4f);

        // Zapneme
        if (ai != null) ai.enabled = true;
        if (warriorAI != null) warriorAI.enabled = true;
        if (archerAI != null) archerAI.enabled = true;
        if (slimeAI != null) slimeAI.enabled = true;
        // if (bossAI != null) bossAI.enabled = true;
    }

    int CalculateXP()
    {
        int baseVal = 0;
        int scaleVal = 0;

        switch (rank)
        {
            case EnemyRank.Weak:    // Slime
                baseVal = 10; scaleVal = 2; break;
            case EnemyRank.Normal:  // Skeleton
                baseVal = 30; scaleVal = 5; break;
            case EnemyRank.Elite:   // Ent
                baseVal = 80; scaleVal = 10; break;
            case EnemyRank.Boss:    // Boss
                baseVal = 500; scaleVal = 50; break;
        }

        return baseVal + ((level - 1) * scaleVal);
    }

    int ApplyLevelGapPenalty(int xpAmount)
    {
        if (PlayerStats.instance == null) return xpAmount;

        int playerLvl = PlayerStats.instance.currentLevel;
        int enemyLvl = level;

        if (enemyLvl >= playerLvl) return xpAmount;

        int diff = playerLvl - enemyLvl;
        if (diff <= 3) return xpAmount;

        float penalty = (diff - 3) * 0.2f;
        float multiplier = 1.0f - penalty;

        if (multiplier <= 0) return 1;

        return Mathf.RoundToInt(xpAmount * multiplier);
    }

    void DropLoot()
    {
        if (lootPrefab == null || lootTable == null) return;

        float playerLuck = 1.0f;
        if (PlayerStats.instance != null) playerLuck = PlayerStats.instance.luck;

        foreach (LootEntry entry in lootTable)
        {
            // Výpoèet šance se štìstím
            float chance = entry.dropChance * playerLuck;

            // --- OPRAVA ZDE ---
            // Pùvodnì jsi porovnával s 'entry.dropChance' místo s vypoèítanou 'chance'
            if (UnityEngine.Random.Range(0f, 100f) <= chance)
            {
                Vector3 spawnPos = transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * scatterRadius;
                GameObject loot = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
                LootPickup pickup = loot.GetComponent<LootPickup>();
                if (pickup != null) pickup.SetItem(entry.item);
            }
        }
    }
}