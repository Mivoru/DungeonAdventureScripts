using UnityEngine;

public enum ItemType
{
    Material,
    Consumable,
    Weapon,
    Tool
}

// --- NOVÉ: Typ spotøebního pøedmìtu ---
public enum ConsumableType
{
    Health,      // Doplòuje životy
    Mana,        // Doplòuje manu (do budoucna)
    DamageBoost, // Zvyšuje sílu
    SpeedBoost,  // Zvyšuje rychlost
    None         // Není to potion (jídlo, atd.)
}
// --------------------------------------

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon;
    [TextArea] public string description;

    [Header("Type")]
    public ItemType itemType;

    // --- NOVÉ ---
    [Header("Consumable Settings")]
    public ConsumableType consumableType; // Tady v Inspectoru vybereš "Health" nebo "DamageBoost"
    // ------------

    [Header("Action Stats")]
    public int valueAmount;
    public string weaponID;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 100;

    [Header("Economy")]
    public int price = 10;

    [Header("Crafting / Smelting")]
    public float burnDuration = 0f;
}