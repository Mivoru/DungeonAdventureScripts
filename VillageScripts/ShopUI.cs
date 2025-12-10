using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    private bool isOpening = false;

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    void Update()
    {
        if (isOpening) return;

        if (shopPanel.activeSelf && currentShopkeeper != null && PlayerStats.instance != null)
        {
            float dist = Vector2.Distance(PlayerStats.instance.transform.position, currentShopkeeper.transform.position);
            if (dist > 3.0f) CloseShop();
        }
    }

    public void ToggleShop(Shopkeeper shopkeeper)
    {
        if (shopPanel.activeSelf && currentShopkeeper == shopkeeper) CloseShop();
        else OpenShop(shopkeeper);
    }

    public void OpenShop(Shopkeeper shopkeeper)
    {
        currentShopkeeper = shopkeeper;
        if (shopNameText) shopNameText.text = shopkeeper.shopName;
        UpdateCoinsText();
        GenerateShopItems();
        StartCoroutine(ForceOpenRoutine());
    }

    IEnumerator ForceOpenRoutine()
    {
        isOpening = true;
        shopPanel.SetActive(true);

        if (InventoryManager.instance != null)
            InventoryManager.instance.OpenInventoryForTrading(inventoryTradePosition);

        yield return new WaitForEndOfFrame();
        shopPanel.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shopPanel.GetComponent<RectTransform>());
        yield return new WaitForSeconds(0.2f);
        isOpening = false;
    }

    public void CloseShop()
    {
        if (isOpening) return;

        shopPanel.SetActive(false);
        currentShopkeeper = null;

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.ResetInventoryPosition();
            // --- PØIDÁNO: Zrušit drag ---
            InventoryManager.instance.CancelActiveDrag();
        }
    }

    public void TrySellItem(ItemData item, int slotIndex)
    {
        if (!shopPanel.activeSelf) return;
        int sellPrice = Mathf.Max(1, item.price / 2);
        if (PlayerStats.instance != null) PlayerStats.instance.currentCoins += sellPrice;
        if (InventoryManager.instance != null) InventoryManager.instance.RemoveItem(slotIndex, 1);
        UpdateCoinsText();
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
            }
        }
    }

    void UpdateCoinsText()
    {
        if (PlayerStats.instance != null && playerCoinsText != null)
            playerCoinsText.text = $"Coins: {PlayerStats.instance.currentCoins}";
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