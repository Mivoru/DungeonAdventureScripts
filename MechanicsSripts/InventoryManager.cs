using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Config")]
    public int inventorySize = 40; // 5x8 slotù
    public GameObject inventoryPanel;
    public Transform slotsContainer;
    public GameObject slotPrefab;
    public GameObject lootPrefab; // Prefab pro vyhazování na zem

    // Vnitøní tøída pro data slotu
    [System.Serializable]
    public class SlotData
    {
        public ItemData item;
        public int amount;
    }

    public SlotData[] slots; // Pole všech slotù (Data)
    private InventorySlot[] uiSlots; // Pole UI prvkù

    private bool isOpen = false;

    void Awake()
    {
        instance = this;
        slots = new SlotData[inventorySize];
        for (int i = 0; i < inventorySize; i++) slots[i] = new SlotData();
    }

    void Start()
    {
        inventoryPanel.SetActive(false);
        CreateUISlots();
    }

    void CreateUISlots()
    {
        uiSlots = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slotScript = obj.GetComponent<InventorySlot>();
            slotScript.slotIndex = i;
            uiSlots[i] = slotScript;

            // --- PØIDAT TENTO ØÁDEK ---
            // Okamžitì vyèistíme vzhled, aby zmizelo "100/100" a defaultní ikona
            slotScript.ClearSlot();
        }
        UpdateUI();
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isOpen = !isOpen;

            if (isOpen)
            {
                inventoryPanel.SetActive(true);
                UpdateUI();
            }
            else
            {
                // --- ZÁCHRANNÁ BRZDA ---
                // Pokud hráè nìco drží v ruce a zavírá inventáø, vrátíme to zpìt do slotu
                if (DraggableItem.itemBeingDragged != null)
                {
                    DraggableItem.itemBeingDragged.FinishDrag();
                }
                // -----------------------

                inventoryPanel.SetActive(false);
            }
        }
    }

    // === HLAVNÍ LOGIKA PØIDÁVÁNÍ ===
    public bool AddItem(ItemData item, int amount = 1)
    {
        // 1. Zkusíme pøidat do existujícího stacku (pokud je stackable)
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStackSize)
                {
                    int spaceLeft = item.maxStackSize - slots[i].amount;
                    int toAdd = Mathf.Min(spaceLeft, amount);

                    slots[i].amount += toAdd;
                    amount -= toAdd;

                    if (amount <= 0)
                    {
                        UpdateUI();
                        return true; // Všechno se vešlo
                    }
                }
            }
        }

        // 2. Zbytek dáme do prázdných slotù
        while (amount > 0)
        {
            int emptyIndex = FindFirstEmptySlot();
            if (emptyIndex == -1) return false; // Inventáø je plný

            slots[emptyIndex].item = item;
            int toAdd = Mathf.Min(item.maxStackSize, amount);
            slots[emptyIndex].amount = toAdd;
            amount -= toAdd;
        }

        UpdateUI();
        return true;
    }

    int FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == null) return i;
        }
        return -1;
    }

    // === PØESOUVÁNÍ A DROP ===
    public void SwapItems(int indexA, int indexB)
    {
        SlotData temp = slots[indexA];
        slots[indexA] = slots[indexB];
        slots[indexB] = temp;
        UpdateUI();
    }

    public void DropItem(int slotIndex)
    {
        if (slots[slotIndex].item != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // --- OPRAVA ZDE: Pøidáno "UnityEngine." ---
                Vector3 dropPos = player.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.5f;

                GameObject loot = Instantiate(lootPrefab, dropPos, Quaternion.identity);
                LootPickup pickup = loot.GetComponent<LootPickup>();

                if (pickup != null)
                {
                    pickup.SetItem(slots[slotIndex].item, slots[slotIndex].amount);
                    pickup.pickupDelay = 2.0f;
                }
            }

            slots[slotIndex].item = null;
            slots[slotIndex].amount = 0;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            uiSlots[i].UpdateSlot(slots[i].item, slots[i].amount);
        }
    }
}