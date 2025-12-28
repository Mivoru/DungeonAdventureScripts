using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Nutné pro LayoutRebuilder
using TMPro;

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
    public RectTransform inventoryRect;
    private Vector2 defaultPosition;

    [Header("Starting Items")]
    public List<ItemData> startingItems;

    [Header("Tooltip")]
    public GameObject tooltipPanel;
    public TMP_Text tooltipText;
    public Vector3 tooltipOffset = new Vector3(15, -15, 0); // Posun od myši
    [Header("Item Database")]
    public List<ItemData> allGameItems;

    private string encryptionKey = "Moje46Super56Tajne66Heslo123";

    private string EncryptDecrypt(string text)
    {
        // Jednoduchá XOR šifra
        System.Text.StringBuilder modifiedData = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            modifiedData.Append((char)(text[i] ^ encryptionKey[i % encryptionKey.Length]));
        }
        return modifiedData.ToString();
    }
    // --- Metody pro SaveManager ---

    public List<InventorySaveData> GetInventorySaveData()
    {
        List<InventorySaveData> saveDataList = new List<InventorySaveData>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null)
            {
                InventorySaveData data = new InventorySaveData();
                data.itemName = slots[i].item.itemName; // Ukládáme jméno
                data.amount = slots[i].amount;
                data.slotIndex = i;
                saveDataList.Add(data);
            }
        }
        return saveDataList;
    }

    public void LoadInventoryFromSave(List<InventorySaveData> savedItems)
    {
        ClearInventory(); // Nejdøív vše vymažeme

        foreach (InventorySaveData savedSlot in savedItems)
        {
            // Najdeme ItemData podle jména v naší "databázi"
            ItemData foundItem = allGameItems.Find(x => x.itemName == savedSlot.itemName);

            if (foundItem != null)
            {
                // Vložíme ho pøesnì na stejné místo
                if (savedSlot.slotIndex < slots.Length)
                {
                    slots[savedSlot.slotIndex].item = foundItem;
                    slots[savedSlot.slotIndex].amount = savedSlot.amount;
                }
            }
            else
            {
                Debug.LogWarning("Nenašel jsem item: " + savedSlot.itemName + ". Máš ho v allGameItems?");
            }
        }
        UpdateUI();
    }
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
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject.transform.root.gameObject);

        slots = new SlotData[inventorySize];
        for (int i = 0; i < inventorySize; i++) slots[i] = new SlotData();

        if (inventoryPanel == null)
        {
            Transform p = transform.Find("InventoryPanel");
            if (p != null) inventoryPanel = p.gameObject;
        }

        if (inventoryPanel != null)
        {
            inventoryRect = inventoryPanel.GetComponent<RectTransform>();
            if (inventoryRect != null) defaultPosition = inventoryRect.anchoredPosition;
            inventoryPanel.SetActive(false);
        }
    }

    void Start()
    {
        CreateUISlots();
        if (IsInventoryEmpty() && startingItems != null)
        {
            foreach (var item in startingItems)
            {
                if (item != null) AddItem(item, 1);
            }
        }
    }

    void CreateUISlots()
    {
        foreach (Transform child in slotsContainer) Destroy(child.gameObject);

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

    bool IsInventoryEmpty()
    {
        foreach (var slot in slots)
        {
            if (slot.item != null) return false;
        }
        return true;
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        // TVRDÁ POJISTKA PROTI 'E' (aby se nepral s obchodem)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            return;
        }

        if (context.performed)
        {
            // Pokud je obchod nebo pec otevøená, zavøeme je
            if (ShopUI.instance != null && ShopUI.instance.shopPanel.activeSelf)
            {
                ShopUI.instance.CloseShop();
                return;
            }
            if (FurnaceUI.instance != null && FurnaceUI.instance.panel.activeSelf)
            {
                FurnaceUI.instance.CloseFurnace();
                return;
            }

            ToggleInventoryNormal();
        }
    }

    public void ToggleInventoryNormal()
    {
        isOpen = !isOpen;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);

            // --- NOVÉ: Ovládání hodin ---
            // Když je inventáø otevøený (isOpen == true), hodiny se schovají (ShowClock false)
            if (TimeUI.instance != null)
            {
                TimeUI.instance.ShowClock(!isOpen);
            }
            // ---------------------------

            if (isOpen)
            {
                if (inventoryRect != null) inventoryRect.anchoredPosition = defaultPosition;
                UpdateUI();
            }
            else
            {
                CancelActiveDrag(); // Zrušit pøetahování pøi zavøení
            }
        }
    }
    public bool AddItem(ItemData item, int amount = 1)
    {
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
                    if (amount <= 0) { UpdateUI(); return true; }
                }
            }
        }

        while (amount > 0)
        {
            int emptyIndex = FindFirstEmptySlot();
            if (emptyIndex == -1) return false;

            slots[emptyIndex].item = item;
            int add = Mathf.Min(item.maxStackSize, amount);
            slots[emptyIndex].amount = add;
            amount -= add;
        }
        AudioManager.instance.PlaySFX("PlayerPickup");
        UpdateUI();
        return true;
    }

    int FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++) { if (slots[i].item == null) return i; }
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
        if (uiSlots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < uiSlots.Length && uiSlots[i] != null)
            {
                uiSlots[i].UpdateSlot(slots[i].item, slots[i].amount);
            }

            // Aktualizace Hotbaru (pokud existuje)
            if (i < 6 && HotbarManager.instance != null)
            {
                HotbarManager.instance.RefreshHotbarSlot(i, slots[i].item, slots[i].amount);
            }
        }
    }

    public void RemoveItem(int slotIndex, int amountToRemove)
    {
        if (slots[slotIndex].item != null)
        {
            slots[slotIndex].amount -= amountToRemove;
            if (slots[slotIndex].amount <= 0)
            {
                slots[slotIndex].item = null;
                slots[slotIndex].amount = 0;
            }
            UpdateUI();
        }
    }

    public SlotData GetSlotData(int index) { return slots[index]; }

    public void SaveSnapshot()
    {
        savedSlots.Clear();
        foreach (var slot in slots) savedSlots.Add(new SlotData { item = slot.item, amount = slot.amount });
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
            else { slots[i].item = null; slots[i].amount = 0; }
        }
        UpdateUI();
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Length; i++) { slots[i].item = null; slots[i].amount = 0; }
        UpdateUI();
    }

    public void OpenInventoryForTrading(Vector2 offsetPosition)
    {
        isOpen = true;
        inventoryPanel.SetActive(true);

        // --- NOVÉ: Schovat hodiny (pro jistotu) ---
        if (TimeUI.instance != null) TimeUI.instance.ShowClock(false);
        // ------------------------------------------

        if (inventoryRect != null)
        {
            inventoryRect.anchoredPosition = offsetPosition;
            LayoutRebuilder.ForceRebuildLayoutImmediate(inventoryRect);
        }
        UpdateUI();
    }

    // --- NOVÁ METODA: Zrušení pøetahování ---
    public void CancelActiveDrag()
    {
        if (DraggableItem.itemBeingDragged != null)
        {
            DraggableItem.itemBeingDragged.FinishDrag();
            DraggableItem.itemBeingDragged = null;
        }
    }

    public void ResetInventoryPosition()
    {
        if (inventoryRect != null) inventoryRect.anchoredPosition = defaultPosition;
        isOpen = false;
        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        // --- NOVÉ: Zobrazit hodiny zpìt ---
        if (TimeUI.instance != null) TimeUI.instance.ShowClock(true);
        // ----------------------------------

        // Zrušit drag, aby item nezùstal viset
        CancelActiveDrag();
    }

    public void UseItem(int slotIndex)
    {
        if (slots[slotIndex].item == null) return;
        ItemData item = slots[slotIndex].item;

        switch (item.itemType)
        {
            case ItemType.Consumable: ConsumeItem(item, slotIndex); break;
            case ItemType.Weapon:
            case ItemType.Tool: EquipItem(item); break;
            case ItemType.Material: Debug.Log("S tímto pøedmìtem nejde nic dìlat."); break;
        }
    }

    void ConsumeItem(ItemData item, int slotIndex)
    {
        if (PlayerStats.instance != null)
        {
            // Tady to házelo chybu, pokud byl Heal 'void'. 
            // Teï když je 'bool', bude to fungovat.
            bool success = PlayerStats.instance.Heal(item.valueAmount);
            AudioManager.instance.PlaySFX("PotionUse");
            if (success)
            {
                RemoveItem(slotIndex, 1);
            }
        }
    }

    void EquipItem(ItemData item)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            WeaponManager wm = player.GetComponentInChildren<WeaponManager>();
            if (wm != null) wm.EquipWeaponByID(item.weaponID);
        }
    }
    // --- CRAFTING HELPERS ---

    // Zjistí celkový poèet tohoto itemu v celém inventáøi
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        foreach (var slot in slots)
        {
            if (slot.item == item) count += slot.amount;
        }
        return count;
    }

    // Odebere konkrétní poèet (postupnì z více stackù)
    public void RemoveItemByName(ItemData item, int amountToRemove)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item == item)
            {
                int take = Mathf.Min(slots[i].amount, amountToRemove);
                slots[i].amount -= take;
                amountToRemove -= take;

                if (slots[i].amount <= 0)
                {
                    slots[i].item = null;
                    slots[i].amount = 0;
                }

                if (amountToRemove <= 0) break;
            }
        }
        UpdateUI();
    }
    void Update()
    {
        // ... (pokud tam máš nìco jiného, nech to tam) ...

        // Logika pro pohyb Tooltipu za myší
        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            // Posuneme panel na pozici myši + offset
            tooltipPanel.transform.position = Mouse.current.position.ReadValue() + (Vector2)tooltipOffset;
        }
       
        // --- NOVÉ: QUICK HEAL (H) ---
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            TryQuickHeal();
        }
    }

    // Tuto metodu zavolá Slot, když na nìj najedeš
    public void ShowTooltip(string itemName)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = itemName;
            tooltipPanel.SetActive(true);

            // Zajistíme, že je tooltip vykreslený úplnì nahoøe (nad sloty)
            tooltipPanel.transform.SetAsLastSibling();
        }
    }

    // Tuto metodu zavolá Slot, když z nìj odjedeš
    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }

    public void TryQuickHeal()
    {
        // Projdeme sloty a hledáme KONKRÉTNÌ Health Potion
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].item != null &&
                slots[i].item.itemType == ItemType.Consumable &&
                slots[i].item.consumableType == ConsumableType.Health) // Kontrola typu!
            {
                // Našli jsme Health Potion -> Použijeme ho
                UseItem(i);
                // Volitelné: Pøehrát zvuk pití
                Debug.Log($"Quick Heal: Použit {slots[i].item.itemName}");
                return; // Použijeme jen jeden a konèíme
            }
        }

        Debug.Log("Nemáš žádný Health Potion!");
        // Tady by se hodilo pøehrát zvuk "Error" nebo vyhodit text na obrazovku
    }
}