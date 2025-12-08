using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HotbarManager : MonoBehaviour
{
    public static HotbarManager instance;

    [Header("Config")]
    public int hotbarSize = 6;
    public int selectedSlotIndex = 0;

    [Header("UI References")]
    public Transform hotbarContainer;
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    // TOTO JE TA PROMÌNNÁ, KTERÁ TI CHYBÌLA (patøí sem, ne do InventoryManageru)
    private InventorySlot[] hotbarSlots;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    void Start()
    {
        hotbarSlots = hotbarContainer.GetComponentsInChildren<InventorySlot>();
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].slotIndex = i;
        }
        SelectSlot(0);
    }

    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        selectedSlotIndex = index;
        UpdateVisuals();
        EquipItemInSlot(selectedSlotIndex);
    }

    void UpdateVisuals()
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            Image slotBg = hotbarSlots[i].GetComponent<Image>();
            if (slotBg != null)
            {
                slotBg.color = (i == selectedSlotIndex) ? selectedColor : normalColor;
            }
        }
    }

    void EquipItemInSlot(int index)
    {
        if (InventoryManager.instance == null) return;
        var slotData = InventoryManager.instance.GetSlotData(index);

        if (slotData != null && slotData.item != null)
        {
            if (slotData.item.itemType == ItemType.Weapon || slotData.item.itemType == ItemType.Tool)
            {
                InventoryManager.instance.UseItem(index);
            }
        }
    }

    // TOTO JE TA METODA, KTEROU VOLÁ INVENTORY MANAGER
    public void RefreshHotbarSlot(int index, ItemData item, int amount)
    {
        if (hotbarSlots != null && index >= 0 && index < hotbarSlots.Length && hotbarSlots[index] != null)
        {
            hotbarSlots[index].UpdateSlot(item, amount);
        }
    }
}