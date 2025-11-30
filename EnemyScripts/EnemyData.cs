using UnityEngine;
using System.Collections.Generic;

// Tento pøíkaz pøidá možnost "Create -> Enemy Data" do menu v Unity
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Dungeon/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Základní Info")]
    public string enemyName = "Monster";

    [Header("Statistiky (Base)")]
    public int maxHealth = 100;
    public int baseDamage = 10;
    public int xpReward = 20;

    [Header("Pohyb")]
    public float walkSpeed = 2f;
    public float runSpeed = 4f;

    [Header("AI Vzdálenosti")]
    public float aggroRange = 10f;
    public float attackRange = 1.5f; // Kdy útoèí
    public float stopDistance = 1.2f;
    public float fleeDistance = 0f;  // 0 pro Warriora, >0 pro Archera

    [Header("Loot")]
    public GameObject lootPrefab; // Univerzální prefab
    public List<EnemyStats.LootEntry> lootTable; // Seznam dropù
}