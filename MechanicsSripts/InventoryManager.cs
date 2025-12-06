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
    // SNAPSHOT DATA
    private List<SlotData> savedSlots = new List<SlotData>();

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
    // Zavolá GameManager pøi vstupu do dungeonu
    public void SaveSnapshot()
    {
        savedSlots.Clear();
        foreach (var slot in slots)
        {
            // Musíme vytvoøit KOPII dat, ne jen odkaz!
            SlotData copy = new SlotData();
            copy.item = slot.item;
            copy.amount = slot.amount;
            savedSlots.Add(copy);
        }
        Debug.Log("Inventáø uložen (Snapshot created).");
    }

    // Zavolá GameManager pøi smrti (Normal Mode)
    public void LoadSnapshot()
    {
        if (savedSlots.Count == 0) return;

        for (int i = 0; i < inventorySize; i++)
        {
            if (i < savedSlots.Count)
            {
                slots[i].item = savedSlots[i].item;
                slots[i].amount = savedSlots[i].amount;
            }
            else
            {
                slots[i].item = null;
                slots[i].amount = 0;
            }
        }
        UpdateUI();
        Debug.Log("Inventáø obnoven ze Snapshotu.");
    }

    // Zavolá GameManager pøi smrti (Hard Mode)
    public void ClearInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].amount = 0;
        }
        UpdateUI();
        Debug.Log("HARD MODE: Inventáø vymazán!");
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