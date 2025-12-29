using System.Collections.Generic;
using UnityEngine;
using TMPro;

// 1. Definujeme si druhy nep¯·tel
public enum EnemyRank
{
    Weak,   // Slime, Krysa (M·lo XP)
    Normal, // Skeleton Warrior/Archer (St¯ednÌ XP)
    Elite,  // Ent, Miniboss (HodnÏ XP)
    Boss    // HlavnÌ boss (Ob¯Ì XP)
}

public class EnemyStats : CharacterStats
{
    [Header("Enemy Data")]
    public EnemyData data;

    [Header("Enemy Info")]
    public string enemyName = "Enemy";
    public EnemyRank rank = EnemyRank.Weak; // Tady v Inspectoru vybereö, co to je

    [Header("Enemy Level")]
    public int level = 1;

    [Header("Scaling Settings")]
    public int healthPerLevel = 10;
    public int damagePerLevel = 2;
    // xpRewardPerLevel uû nepot¯ebujeme tolik ¯eöit, vypoËÌt·me to dynamicky podle Ranku

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

    public override void TakeDamage(int damage, bool isCrit = false)
    {
        GiantAI giant = GetComponent<GiantAI>();
        if (giant != null && giant.IsInvulnerable())
        {
            Debug.Log(" GIANT JE NEZRANITELN›!");
            return;
        }
        SkeletonArcherAI archerAI = GetComponent<SkeletonArcherAI>();
        if (archerAI != null && archerAI.IsEvading()) return;

        Animator anim = GetComponent<Animator>();
        SkeletonWarriorAI warriorAI = GetComponent<SkeletonWarriorAI>();
        if (warriorAI != null && anim != null && anim.GetBool("IsBlocking")) return;
        // ZAVOL¡ME FLOATING TEXT
        if (FloatingTextManager.instance != null)
        {
            FloatingTextManager.instance.ShowDamage(damage, transform.position, isCrit);
        }

        base.TakeDamage(damage, isCrit); // Zavol· CharacterStats (ubere HP)
        if (enemyAudio != null && currentHealth > 0)
        {
            enemyAudio.PlayHurt();
        }

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
        if (enemyAudio != null)
        {
            // Trik: Vytvo¯Ìme doËasn˝ objekt jen pro zvuk smrti, pokud se tento zniËÌ
            AudioSource.PlayClipAtPoint(enemyAudio.deathSound, transform.position);

            // Nebo pokud pouûÌv·ö animaci smrti a Destroy je zpoûdÏnÈ, staËÌ:
            // enemyAudio.PlayDeath(); 
        }
        base.Die();

        if (PlayerStats.instance != null)
        {
            // 1. VypoËÌt·me XP podle Ranku a Levelu nep¯Ìtele
            int xpAmount = CalculateXP();

            // 2. Aplikujeme penalizaci, pokud je hr·Ë moc siln˝
            int finalXP = ApplyLevelGapPenalty(xpAmount);

            Debug.Log($"Enemy {rank} (Lvl {level}) killed. Base: {xpAmount}, Final: {finalXP}");

            PlayerStats.instance.AddXP(finalXP);
        }

        DropLoot();
        Destroy(gameObject);
    }
    int CalculateXP()
    {
        int baseVal = 0;
        int scaleVal = 0;

        // NastavenÌ hodnot podle toho, jak˝ je to typ nep¯Ìtele
        switch (rank)
        {
            case EnemyRank.Weak:    // Slime
                baseVal = 10;
                scaleVal = 2;       // +2 XP za kaûd˝ level navÌc
                break;
            case EnemyRank.Normal:  // Skeleton
                baseVal = 30;
                scaleVal = 5;       // +5 XP za level
                break;
            case EnemyRank.Elite:   // Ent
                baseVal = 80;
                scaleVal = 10;
                break;
            case EnemyRank.Boss:    // Boss
                baseVal = 500;
                scaleVal = 50;
                break;
        }

        // Vzorec: Z·klad + (Level nep¯Ìtele * ök·lov·nÌ)
        return baseVal + ((level - 1) * scaleVal);
    }

    int ApplyLevelGapPenalty(int xpAmount)
    {
        int playerLvl = PlayerStats.instance.currentLevel;
        int enemyLvl = level; // Tady se bere TENHLE level tohoto nep¯Ìtele

        // Pokud je nep¯Ìtel silnÏjöÌ nebo stejn˝, dostaneö 100% XP
        if (enemyLvl >= playerLvl) return xpAmount;

        // Pokud je hr·Ë silnÏjöÌ, poËÌt·me rozdÌl
        int diff = playerLvl - enemyLvl;

        // Tolerance: Pokud jsi o 1-3 levely v˝ö, jeötÏ ti XP nesebere
        if (diff <= 3) return xpAmount;

        // Pokud je rozdÌl vÏtöÌ neû 3 levely:
        // Za kaûd˝ dalöÌ level dol˘ -20% XP
        // P¯Ìklad: Hr·Ë 15, Enemy 5 -> RozdÌl 10. (10 - 3 tolerance) = 7.
        // 7 * 0.2 = 1.4 (140% penalizace) -> 0 XP.

        float penalty = (diff - 3) * 0.2f;
        float multiplier = 1.0f - penalty;

        if (multiplier <= 0) return 1; // Vûdy dostaneö aspoÚ 1 XP (symbolicky)

        return Mathf.RoundToInt(xpAmount * multiplier);
    }
    void DropLoot()
    {
        if (lootPrefab == null || lootTable == null) return;

        // ZÌsk·me hr·Ëovo ötÏstÌ
        float playerLuck = 1.0f;
        if (PlayerStats.instance != null) playerLuck = PlayerStats.instance.luck;

        foreach (LootEntry entry in lootTable)
        {
            // ZMÃNA: N·sobÌme öanci ötÏstÌm
            // (Nap¯. 10% öance * 1.5 Luck = 15% öance)
            float chance = entry.dropChance * playerLuck;
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