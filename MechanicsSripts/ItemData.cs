using UnityEngine;

public enum ItemType
{
    Material,   // Suroviny (Kosti, Gel, Rudy) - Nedìlají nic
    Consumable, // Spotøební (Lektvary) - Zmizí a dají efekt
    Weapon,     // Zbranì (Meè, Luk) - Nasadí se
    Tool        // Nástroje (Krumpáè) - Nasadí se
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon;
    [TextArea] public string description;

    [Header("Type")]
    public ItemType itemType;

    [Header("Action Stats")]
    public int valueAmount; // Pro Potion = Heal, Pro Zbraò = Damage
    public string weaponID; // "Sword", "Bow", "Pickaxe" (Pro spárování s WeaponManagerem)

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 100;

    [Header("Economy")]
    public int price = 10;
}