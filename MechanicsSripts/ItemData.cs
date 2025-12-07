using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName = "New Item";
    public Sprite icon;
    [TextArea] public string description;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 100; // Kolik se jich vejde na sebe

    [Header("Economy")]
    public int price = 10;
}