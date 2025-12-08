using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems; // Nutné pro klikání

// Pøidali jsme IPointerClickHandler, aby slot reagoval na kliknutí myší
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
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.color = Color.white;
                iconImage.enabled = true;
            }

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
            iconImage.color = Color.clear;
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
                InventoryManager.instance.SwapItems(droppedItem.parentSlot.slotIndex, slotIndex);
                droppedItem.FinishDrag();
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ShopUI.instance != null && ShopUI.instance.shopPanel.activeSelf)
            {
                // ... (Prodej - to už máš) ...
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);
                if (slotData != null) ShopUI.instance.TrySellItem(slotData.item, slotIndex);
            }
            else
            {
                // --- NOVÉ: POUŽÍT / VYBAVIT ---
                InventoryManager.instance.UseItem(slotIndex);
            }
        }
    }
}