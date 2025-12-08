using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections; // Nutné pro Coroutiny
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    public static ShopUI instance;

    [Header("References")]
    public GameObject shopPanel;
    public Transform itemsContainer;
    public GameObject shopItemPrefab;
    public TMP_Text shopNameText;
    public TMP_Text playerCoinsText;

    [Header("Layout Settings")]
    public Vector2 inventoryTradePosition = new Vector2(400, 0);

    private Shopkeeper currentShopkeeper;
    private bool isOpening = false; // Pojistka

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        // Hned v Awake vypneme panel, aby nestrašil
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Start()
    {
        // Necháme prázdné, aby nám to nepøepisovalo stav
    }

    void Update()
    {
        // POJISTKA: Pokud se obchod právì otevírá (prvních pár milisekund), nekontrolujeme vzdálenost
        if (isOpening) return;

        // Auto-zavøení pøi vzdálení
        if (shopPanel.activeSelf && currentShopkeeper != null && PlayerStats.instance != null)
        {
            float dist = Vector2.Distance(PlayerStats.instance.transform.position, currentShopkeeper.transform.position);

            // Pokud jsi dál než 3 metry, zavøít
            if (dist > 3.0f)
            {
                CloseShop();
            }
        }
    }

    public void ToggleShop(Shopkeeper shopkeeper)
    {
        if (shopPanel.activeSelf && currentShopkeeper == shopkeeper)
        {
            CloseShop();
        }
        else
        {
            OpenShop(shopkeeper);
        }
    }

    public void OpenShop(Shopkeeper shopkeeper)
    {
        Debug.Log("SHOP: Otevírám obchod...");
        currentShopkeeper = shopkeeper;

        if (shopNameText) shopNameText.text = shopkeeper.shopName;
        UpdateCoinsText();
        GenerateShopItems();

        // Spustíme "bezpeèné" otevøení
        StartCoroutine(ForceOpenRoutine());
    }

    // --- NOVÁ POJISTKA PROTI ZAVØENÍ ---
    IEnumerator ForceOpenRoutine()
    {
        isOpening = true; // Zapneme ochranu proti Update()

        // 1. Zapneme panel hned
        shopPanel.SetActive(true);

        // Otevøeme inventáø
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.OpenInventoryForTrading(inventoryTradePosition);
        }

        // 2. Poèkáme na konec snímku (aby probìhly všechny Starty a Layouty)
        yield return new WaitForEndOfFrame();

        // 3. PRO JISTOTU ZNOVU ZAPNEME (Kdyby ho nìkdo vypnul)
        shopPanel.SetActive(true);

        // Vynutíme pøekreslení (proti glitchùm v grafice)
        LayoutRebuilder.ForceRebuildLayoutImmediate(shopPanel.GetComponent<RectTransform>());

        // 4. Poèkáme chvilku (0.2s), než zaèneme kontrolovat vzdálenost
        yield return new WaitForSeconds(0.2f);

        isOpening = false; // Vypneme ochranu
    }

    public void CloseShop()
    {
        // Pokud se zrovna otevírá, nedovolíme ho zavøít
        if (isOpening) return;

        Debug.Log("SHOP: Zavírám obchod.");
        shopPanel.SetActive(false);
        currentShopkeeper = null;

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ResetInventoryPosition();
        }
    }

    // ... ZBYTEK KÓDU (TrySellItem, TryBuyItem, atd.) NECH STEJNÝ ...

    public void TrySellItem(ItemData item, int slotIndex)
    {
        if (!shopPanel.activeSelf) return;

        int sellPrice = Mathf.Max(1, item.price / 2);
        if (PlayerStats.instance != null) PlayerStats.instance.currentCoins += sellPrice;
        if (InventoryManager.instance != null) InventoryManager.instance.RemoveItem(slotIndex, 1);

        UpdateCoinsText();
        Debug.Log($"Prodáno: {item.itemName}");
    }

    public void TryBuyItem(ItemData item)
    {
        if (PlayerStats.instance == null || InventoryManager.instance == null) return;

        if (PlayerStats.instance.currentCoins >= item.price)
        {
            if (InventoryManager.instance.AddItem(item, 1))
            {
                PlayerStats.instance.currentCoins -= item.price;
                UpdateCoinsText();
                Debug.Log($"Koupeno: {item.itemName}");
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

    void UpdateCoinsText()
    {
        if (PlayerStats.instance != null && playerCoinsText != null)
        {
            playerCoinsText.text = $"Coins: {PlayerStats.instance.currentCoins}";
        }
    }

    void GenerateShopItems()
    {
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);
        if (currentShopkeeper == null) return;

        foreach (ItemData item in currentShopkeeper.itemsForSale)
        {
            GameObject obj = Instantiate(shopItemPrefab, itemsContainer);
            ShopItemSlot slot = obj.GetComponent<ShopItemSlot>();
            if (slot != null) slot.Setup(item, this);
        }
    }
}