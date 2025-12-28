using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // --- HRÁÈ ---
    public int currentHealth;
    public int maxHealth;
    public int coins;
    public int xp;
    public int level;
    public int statPoints;

    // Atributy
    public int defense;
    public int damage; // Base damage
    public float critChance;
    public float critDamage;
    public float attackSpeed;
    public float dashCooldownRed;
    public float luck;
    public float regeneration;

    // --- SVÌT ---
    public int daysPassed;
    public float timeOfDay;
    public List<string> unlockedFloors; // Názvy odemèených pater

    // --- INVENTÁØ ---
    public List<InventorySaveData> inventoryItems;
}

[System.Serializable]
public class InventorySaveData
{
    public string itemName; // Ukládáme název, podle nìj najdeme ItemData
    public int amount;
    public int slotIndex;
}