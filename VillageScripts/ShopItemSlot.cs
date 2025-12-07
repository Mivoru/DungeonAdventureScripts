using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public Button buyButton;

    private ItemData myItem;
    private ShopUI shopManager;

    public void Setup(ItemData item, ShopUI manager)
    {
        myItem = item;
        shopManager = manager;

        iconImage.sprite = item.icon;
        nameText.text = item.itemName;
        priceText.text = item.price.ToString() + " G";

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(OnBuyClicked);
    }

    void OnBuyClicked()
    {
        shopManager.TryBuyItem(myItem);
    }
}