using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance;

    [Header("Config")]
    public int inventorySize = 40;
    public GameObject inventoryPanel;
    public Transform slotsContainer;
    public GameObject slotPrefab;
    public GameObject lootPrefab;

    [Header("UI Position")]
    public RectTransform inventoryRect; // Pøetáhni sem InventoryPanel (musí mít RectTransform)
    private Vector2 defaultPosition;    // Tady si uložíme, kde byl pùvodnì (uprostøed)

    [System.Serializable]
    public class SlotData
    {
        public ItemData item;
        public int amount;
    }

    public SlotData[] slots;
    private InventorySlot[] uiSlots;
    private bool isOpen = false;

    // Snapshot pro návrat po smrti
    private List<SlotData> savedSlots = new List<SlotData>();

    void Awake()
    {
        // --- OPRAVA SINGLETONU ---
        // Pokud už instance existuje a není to tato, znièíme tento duplikát
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Pokud neexistuje, nastavíme se jako hlavní a pøežijeme naèítání
        instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject); // Zajistíme, že pøežije celý HUD
        // -------------------------

        // Inicializace dat (pouze pokud jsme ten hlavní)
        slots = new SlotData[inventorySize];
        for (int i = 0; i < inventorySize; i++) slots[i] = new SlotData();
    }

    void Start()
    {
        // Pokud panel není pøiøazený, zkusíme ho najít (pojistka)
        if (inventoryPanel == null)
        {
            Transform panelTrans = transform.Find("InventoryPanel"); // Uprav cestu dle hierarchie
            if (panelTrans != null) inventoryPanel = panelTrans.gameObject;
        }

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        

        CreateUISlots();
        if (inventoryRect != null)
        {
            defaultPosition = inventoryRect.anchoredPosition;
        }
    }

    void CreateUISlots()
    {
        // Vyèistíme staré sloty, pokud nìjaké zbyly (pro jistotu)
        foreach (Transform child in slotsContainer)
        {
            Destroy(child.gameObject);
        }

        uiSlots = new InventorySlot[inventorySize];
        for (int i = 0; i < inventorySize; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            InventorySlot slotScript = obj.GetComponent<InventorySlot>();
            slotScript.slotIndex = i;
            uiSlots[i] = slotScript;
            slotScript.ClearSlot();
        }
        UpdateUI();
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.LogError(" BYLA ZAVOLÁNA METODA ONTOGGLEINVENTORY! (To se na E nesmí stát)");
            isOpen = !isOpen;
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(isOpen);
                if (isOpen) UpdateUI();
                else
                {
                    if (DraggableItem.itemBeingDragged != null)
                        DraggableItem.itemBeingDragged.FinishDrag();
                }
            }
            
        }
    
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        // 1. Stackování
        if (item.isStackable)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].item == item && slots[i].amount < item.maxStackSize)
                {
                    int space = item.maxStackSize - slots[i].amount;
                    int add = Mathf.Min(space, amount);
                    slots[i].amount += add;
                    amount -= add;
                    if (amount <= 0)
                    {
                        UpdateUI();
                        return true;
                    }
                }
            }
        }

        // 2. Prázdné sloty
        while (amount > 0)
        {
            int emptyIndex = FindFirstEmptySlot();
            if (emptyIndex == -1) return false;

            slots[emptyIndex].item = item;
            int add = Mathf.Min(item.maxStackSize, amount);
            slots[emptyIndex].amount = add;
            amount -= add;
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
                Vector3 dropPos = player.transform.position + (Vector3)UnityEngine.Random.insideUnitCircle * 1.5f;
                GameObject loot = Instantiate(lootPrefab, dropPos, Quaternion.identity);
                LootPickup pickup = loot.GetComponent<LootPickup>();

                if (pickup != null)
                {
                    pickup.SetItem(slots[slotIndex].item, slots[slotIndex].amount);
                    pickup.pickupDelay = 1.0f;
                }
            }

            slots[slotIndex].item = null;
            slots[slotIndex].amount = 0;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        if (uiSlots == null) return; // Pojistka proti chybì pøi ukonèování

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < uiSlots.Length && uiSlots[i] != null)
            {
                uiSlots[i].UpdateSlot(slots[i].item, slots[i].amount);
            }
        }
    }
    // Tuto metodu zavolá ShopUI pøi prodeji
    public void RemoveItem(int slotIndex, int amountToRemove)
    {
        if (slots[slotIndex].item != null)
        {
            slots[slotIndex].amount -= amountToRemove;

            // Pokud klesne na 0, vymažeme slot
            if (slots[slotIndex].amount <= 0)
            {
                slots[slotIndex].item = null;
                slots[slotIndex].amount = 0;
            }

            UpdateUI();
        }
    }

    // Pomocná metoda pro získání dat slotu
    public SlotData GetSlotData(int index)
    {
        return slots[index];
    }

    // --- SNAPSHOTS (Pro GameManager) ---
    public void SaveSnapshot()
    {
        savedSlots.Clear();
        foreach (var slot in slots)
        {
            SlotData copy = new SlotData { item = slot.item, amount = slot.amount };
            savedSlots.Add(copy);
        }
    }

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
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].amount = 0;
        }
        UpdateUI();
    }
    public void OpenInventoryForTrading(Vector2 offsetPosition)
    {
        isOpen = true;
        inventoryPanel.SetActive(true);
        UpdateUI();

        // Posuneme ho na bok
        if (inventoryRect != null)
        {
            inventoryRect.anchoredPosition = offsetPosition;
        }
    }
    // Tuto metodu zavolá ShopUI pøi zavøení
    public void ResetInventoryPosition()
    {
        // Vrátíme ho na pùvodní místo (na støed)
        if (inventoryRect != null)
        {
            inventoryRect.anchoredPosition = defaultPosition;
        }

        // Volitelné: Mùžeme ho i zavøít, nebo nechat otevøený
        CloseInventory();
    }
    public void CloseInventory()
    {
        isOpen = false;
        inventoryPanel.SetActive(false);
        // Reset pozice pro jistotu, aby pøíštì nebyl šikmo, když ho otevøeš klávesou I
        if (inventoryRect != null) inventoryRect.anchoredPosition = defaultPosition;
    }
}