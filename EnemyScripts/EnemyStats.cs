using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyStats : CharacterStats
{
    [Header("Enemy Data (VolitelnÈ)")]
    public EnemyData data; // Pokud sem nÏco d·ö, p¯epÌöe to hodnoty nÌûe

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

    // InternÌ promÏnnÈ pro bezpeËnÈ ök·lov·nÌ
    private int _startMaxHealth;
    private int _startBaseDamage;
    private bool _initialized = false;

    void Awake()
    {
        // 1. Pokud m·me Data (Scriptable Object), naËteme je a PÿEPÕäEME Inspector
        if (data != null)
        {
            enemyName = data.enemyName;
            maxHealth = data.maxHealth;
            baseDamage = data.baseDamage;

            // Loot naËteme jen pokud v Inspectoru nic nenÌ (aby öel p¯epsat manu·lnÏ)
            if (lootTable == null || lootTable.Count == 0) lootTable = data.lootTable;
            if (lootPrefab == null) lootPrefab = data.lootPrefab;
        }

        // 2. UloûÌme si startovnÌ hodnoty (aù uû jsou z Dat nebo z Inspectoru)
        _startMaxHealth = maxHealth;
        _startBaseDamage = baseDamage;

        // 3. Z·chrann· brzda: Pokud je i teÔ 0, d·me tam aspoÚ nÏco, aby neum¯el hned
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
        UpdateUI();
    }

    public void SetLevel(int newLevel)
    {
        level = newLevel;
        ApplyLevelStats();
    }

    void ApplyLevelStats()
    {
        // Vûdy poËÌt·me od _startMaxHealth, coû je teÔ bezpeËnÏ nastavenÈ v Awake
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

    public override void TakeDamage(int damage)
    {
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        if (archerAI != null && archerAI.IsEvading()) return;

        Animator anim = GetComponent<Animator>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();
        if (warriorAI != null && anim != null && anim.GetBool("IsBlocking")) return;

        base.TakeDamage(damage);

        if (anim != null) anim.SetTrigger("Hit");

        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.TriggerAggro();

        if (warriorAI != null) warriorAI.TriggerAggro();
        if (archerAI != null) archerAI.TriggerAggro();

        SlimeAI slime = GetComponent<SlimeAI>();
        if (slime != null) slime.TriggerHurtAnim();

        StartCoroutine(StunRoutine());
    }

    System.Collections.IEnumerator StunRoutine()
    {
        EnemyAI ai = GetComponent<EnemyAI>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        SlimeAI slimeAI = GetComponent<SlimeAI>();

        if (ai != null) ai.enabled = false;
        if (warriorAI != null) warriorAI.enabled = false;
        if (archerAI != null) archerAI.enabled = false;
        if (slimeAI != null) slimeAI.enabled = false;

        yield return new WaitForSeconds(0.4f);

        if (ai != null) ai.enabled = true;
        if (warriorAI != null) warriorAI.enabled = true;
        if (archerAI != null) archerAI.enabled = true;
        if (slimeAI != null) slimeAI.enabled = true;
    }

    public override void Die()
    {
        base.Die();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            int finalXp = baseXpReward + ((level - 1) * xpRewardPerLevel);
            player.GetComponent<PlayerStats>()?.AddXP(finalXp);
        }
        DropLoot();
        Destroy(gameObject);
    }

    void DropLoot()
    {
        if (lootPrefab == null || lootTable == null) return;
        foreach (LootEntry entry in lootTable)
        {
            if (UnityEngine.Random.Range(0f, 100f) <= entry.dropChance)
            {
                Vector3 spawnPos = transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * scatterRadius;
                GameObject loot = Instantiate(lootPrefab, spawnPos, Quaternion.identity);
                LootPickup pickup = loot.GetComponent<LootPickup>();
                if (pickup != null) pickup.SetItem(entry.item);
            }
        }
    }
}