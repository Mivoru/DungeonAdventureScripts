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
                // 1. Z INVENTÁØE (Staré)
                if (!droppedItem.isFromFurnace)
                {
                    InventoryManager.instance.SwapItems(droppedItem.parentSlot.slotIndex, slotIndex);
                    droppedItem.FinishDrag();
                }
                // 2. Z PECE (Nové)
                else
                {
                    var furnace = droppedItem.furnaceSource;
                    ItemData itemToTake = null;
                    int amountToTake = 0;

                    if (droppedItem.furnaceSlotType == "Output") { itemToTake = furnace.outputItem; amountToTake = furnace.outputAmount; }
                    else if (droppedItem.furnaceSlotType == "Input") { itemToTake = furnace.inputItem; amountToTake = furnace.inputAmount; }
                    else if (droppedItem.furnaceSlotType == "Fuel") { itemToTake = furnace.fuelItem; amountToTake = furnace.fuelAmount; }

                    // ZKUSÍME TO DÁT PØÍMO DO TOHOTO SLOTU
                    bool success = false;
                    var mySlot = InventoryManager.instance.slots[slotIndex];

                    // A) Slot je prázdný -> Vložíme
                    if (mySlot.item == null)
                    {
                        mySlot.item = itemToTake;
                        mySlot.amount = amountToTake;
                        success = true;
                    }
                    // B) Slot má stejný item (a je stackable) -> Pøièteme
                    else if (mySlot.item == itemToTake && itemToTake.isStackable && mySlot.amount < itemToTake.maxStackSize)
                    {
                        // (Zjednodušenì pøièteme vše, pro pøesnost by se mìl øešit limit stacku)
                        mySlot.amount += amountToTake;
                        success = true;
                    }
                    // C) Slot je plný/jiný -> Použijeme klasický AddItem (najde jiné místo)
                    else
                    {
                        success = InventoryManager.instance.AddItem(itemToTake, amountToTake);
                    }

                    if (success)
                    {
                        // Vymažeme z pece
                        if (droppedItem.furnaceSlotType == "Output") { furnace.outputItem = null; furnace.outputAmount = 0; }
                        else if (droppedItem.furnaceSlotType == "Input") { furnace.inputItem = null; furnace.inputAmount = 0; }
                        else if (droppedItem.furnaceSlotType == "Fuel") { furnace.fuelItem = null; furnace.fuelAmount = 0; }

                        FurnaceUI.instance.UpdateVisuals();
                        // Musíme aktualizovat UI inventáøe, protože jsme sáhli pøímo do dat slotù
                        InventoryManager.instance.SendMessage("UpdateUI");
                    }
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 1. Je otevøený OBCHOD?
            // ZMÌNA: Místo 'panel' používáme 'shopPanel'
            if (ShopUI.instance != null && ShopUI.instance.shopPanel.activeSelf)
            {
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);
                if (slotData != null && slotData.item != null)
                {
                    ShopUI.instance.TrySellItem(slotData.item, slotIndex);
                }
                return; // Konec
            }

            // 2. Je otevøená PEC?
            if (FurnaceUI.instance != null && FurnaceUI.instance.panel.activeSelf)
            {
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);
                if (slotData != null && slotData.item != null)
                {
                    FurnaceUI.instance.TryMoveItemToFurnace(slotData.item, slotIndex);
                }
                return; // Konec
            }

            // 3. Jinak POUŽÍT/VYBAVIT
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.UseItem(slotIndex);
            }
        }
    }
}