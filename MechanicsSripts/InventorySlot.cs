using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public int slotIndex;

    [Header("UI Components")]
    public Image iconImage;
    public TMP_Text amountText;

    public void UpdateSlot(ItemData item, int amount)
    {
        if (item != null)
        {
            // Nastavení ikony
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.color = Color.white; // DÙLEŽITÉ: Reset barvy na bílou (aby nebyla žlutá/šedá)
                iconImage.enabled = true;
            }

            // Nastavení textu poètu
            if (amountText != null)
            {
                if (amount > 1)
                {
                    amountText.text = amount.ToString();
                    amountText.enabled = true;
                }
                else
                {
                    amountText.enabled = false;
                }
            }
        }
        else
        {
            ClearSlot();
        }
    }

    public void ClearSlot()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear; // Zprùhlednit, aby nebyl vidìt prázdný ètverec
            iconImage.enabled = false;
        }

        if (amountText != null)
        {
            amountText.enabled = false;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableItem droppedItem = eventData.pointerDrag.GetComponent<DraggableItem>();
            if (droppedItem != null)
            {
                // 1. Prohodíme data
                InventoryManager.instance.SwapItems(droppedItem.parentSlot.slotIndex, slotIndex);

                // 2. OPRAVA: Místo Destroy zavoláme FinishDrag
                // To okamžitì vrátí ikonku do pùvodního slotu a resetuje ji.
                // Protože jsme prohodili data, UpdateUI jí hned poté zmìní obrázek na ten správný.
                droppedItem.FinishDrag();
            }
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        // Reagujeme na Pravé tlaèítko myši (Right Click)
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Zkontrolujeme, jestli je obchod otevøený
            if (ShopUI.instance != null && ShopUI.instance.shopPanel.activeSelf)
            {
                // Získáme data o pøedmìtu v tomto slotu
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);

                if (slotData != null && slotData.item != null)
                {
                    // Prodáme ho!
                    ShopUI.instance.TrySellItem(slotData.item, slotIndex);
                }
            }
            else
            {
                // Pokud obchod není otevøený, tady mùžeš dát logiku pro "Equip" nebo "Use" (snìzení lektvaru)
                Debug.Log("Pravý klik: Tady by se item použil/nasadil.");
            }
        }
    }
}