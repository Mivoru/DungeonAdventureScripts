using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public GameObject shopPanel; // Celé okno
    public Transform itemsContainer; // Kam sypat tlaèítka (Content ve ScrollView)
    public GameObject shopItemPrefab; // Vzor tlaèítka zboží
    public TMP_Text shopNameText;
    public TMP_Text playerCoinsText;
    [Header("Layout Settings")]
    // Kam se má posunout inventáø, když obchodujeme? 
    // Zkus tøeba X: 300, Y: 0 (aby byl vpravo od obchodu)
    public Vector2 inventoryTradePosition = new Vector2(400, 0);

    private Shopkeeper currentShopkeeper;
    public static ShopUI instance;

    void Start()
    {
        shopPanel.SetActive(false);
    }
    void Update()
    {
        // Kontrolujeme jen, když je obchod otevøený
        if (shopPanel.activeSelf && currentShopkeeper != null && PlayerStats.instance != null)
        {
            // Zmìøíme vzdálenost mezi Hráèem a Obchodníkem
            float dist = Vector2.Distance(PlayerStats.instance.transform.position, currentShopkeeper.transform.position);

            // Pokud odejdeš dál než 3 metry, obchod se zavøe
            if (dist > 3.0f)
            {
                CloseShop();
            }
        }
    }
    public void ToggleShop(Shopkeeper shopkeeper)
    {
        // Pokud je obchod už otevøený A mluvíme se stejným obchodníkem -> ZAVØÍT
        if (shopPanel.activeSelf && currentShopkeeper == shopkeeper)
        {
            CloseShop();
        }
        else
        {
            // Jinak otevøít (buï bylo zavøeno, nebo mluvíme s jiným obchodníkem)
            OpenShop(shopkeeper);
        }
    }

    public void OpenShop(Shopkeeper shopkeeper)
    {
        currentShopkeeper = shopkeeper;
        shopNameText.text = shopkeeper.shopName;
        UpdateCoinsText();
        GenerateShopItems();

        shopPanel.SetActive(true);

        // --- NOVÉ: OTEVØÍT I INVENTÁØ A POSUNOUT HO ---
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OpenInventoryForTrading(inventoryTradePosition);
        }
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        currentShopkeeper = null;

        // --- NOVÉ: ZAVØÍT INVENTÁØ A VRÁTIT HO ZPÌT ---
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ResetInventoryPosition();
        }
    }

    void UpdateCoinsText()
    {
        if (PlayerStats.instance != null)
        {
            playerCoinsText.text = $"Coins: {PlayerStats.instance.currentCoins}";
        }
    }

    void GenerateShopItems()
    {
        // 1. Smažeme staré položky
        foreach (Transform child in itemsContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Vytvoøíme nové
        foreach (ItemData item in currentShopkeeper.itemsForSale)
        {
            GameObject obj = Instantiate(shopItemPrefab, itemsContainer);

            // Nastavíme vzhled (pøedpokládáme, že prefab má skript ShopItemSlot)
            ShopItemSlot slot = obj.GetComponent<ShopItemSlot>();
            if (slot != null)
            {
                slot.Setup(item, this);
            }
        }
    }

    // Tuto metodu zavolá tlaèítko u zboží
    public void TryBuyItem(ItemData item)
    {
        if (PlayerStats.instance == null || InventoryManager.instance == null) return;

        // 1. Máme dost penìz?
        if (PlayerStats.instance.currentCoins >= item.price)
        {
            // 2. Je místo v inventáøi?
            if (InventoryManager.instance.AddItem(item, 1))
            {
                // Úspìch!
                PlayerStats.instance.currentCoins -= item.price;
                Debug.Log($"Koupeno: {item.itemName}");

                UpdateCoinsText();
                // Pøehrát zvuk cinknutí?
            }
            else
            {
                Debug.Log("Inventáø je plný!");
            }
        }
        else
        {
            Debug.Log("Nemáš dost penìz!");
        }
    }
    public void TrySellItem(ItemData item, int slotIndex)
    {
        // TOTO JE POJISTKA: Prodávat jde jen, když je okno otevøené
        if (!shopPanel.activeSelf) return;

        int sellPrice = Mathf.Max(1, item.price / 2);
        if (PlayerStats.instance != null) PlayerStats.instance.currentCoins += sellPrice;
        if (InventoryManager.instance != null) InventoryManager.instance.RemoveItem(slotIndex, 1);

        UpdateCoinsText();
        Debug.Log($"Prodáno: {item.itemName}");
    }
}