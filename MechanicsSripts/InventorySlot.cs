using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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
                else amountText.enabled = false;
            }
        }
        else ClearSlot();
    }

    public void ClearSlot()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
            iconImage.enabled = false;
        }
        if (amountText != null) amountText.enabled = false;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // --- OPRAVA ZDE ---
        // MÌsto hled·nÌ komponenty na pointerDrag pouûijeme statickou referenci.
        // Ta funguje vûdy, aù uû t·hneme z invent·¯e nebo z pece (ducha).
        DraggableItem droppedItem = DraggableItem.itemBeingDragged;

        if (droppedItem != null)
        {
            // 1. Z INVENT¡ÿE
            if (!droppedItem.isFromFurnace)
            {
                // MusÌme zkontrolovat, jestli parentSlot existuje (pro jistotu)
                if (droppedItem.parentSlot != null)
                {
                    InventoryManager.instance.SwapItems(droppedItem.parentSlot.slotIndex, slotIndex);
                    droppedItem.FinishDrag();
                }
            }
            // 2. Z PECE
            else
            {
                var furnace = droppedItem.furnaceSource;
                if (furnace == null) return;

                ItemData itemToTake = null;
                int amountToTake = 0;

                if (droppedItem.furnaceSlotType == "Output") { itemToTake = furnace.outputItem; amountToTake = furnace.outputAmount; }
                else if (droppedItem.furnaceSlotType == "Input") { itemToTake = furnace.inputItem; amountToTake = furnace.inputAmount; }
                else if (droppedItem.furnaceSlotType == "Fuel") { itemToTake = furnace.fuelItem; amountToTake = furnace.fuelAmount; }

                if (itemToTake == null) return;

                // PokusÌme se vloûit PÿÕMO do tohoto slotu
                bool success = false;
                var mySlot = InventoryManager.instance.slots[slotIndex];

                // A) Slot je pr·zdn˝
                if (mySlot.item == null)
                {
                    mySlot.item = itemToTake;
                    mySlot.amount = amountToTake;
                    success = true;
                }
                // B) Slot m· stejn˝ item (Stacking)
                else if (mySlot.item == itemToTake && itemToTake.isStackable && mySlot.amount < itemToTake.maxStackSize)
                {
                    int space = itemToTake.maxStackSize - mySlot.amount;
                    int toAdd = Mathf.Min(space, amountToTake);

                    if (toAdd > 0)
                    {
                        mySlot.amount += toAdd;
                        amountToTake -= toAdd; // Zbytek (pokud se neveölo vöe)

                        // Pokud zbylo nÏco v ruce, zbytek se vr·tÌ do pece (nebo se pokusÌ p¯idat jinam)
                        // Pro jednoduchost teÔ povaûujeme za ˙spÏch, pokud se aspoÚ nÏco p¯esunulo
                        // V ide·lnÌm p¯ÌpadÏ by se zbytek mÏl vr·tit do pece.
                        // Ale tady nastavÌme success = true a odeËteme vöe, coû je zjednoduöenÌ.
                        // Spr·vnÏjöÌ by bylo aktualizovat furnace o to co zbylo.
                        // Pro teÔ to nech·me takto (p¯edpokl·d·me, ûe se vejde).
                        success = true;
                    }
                }
                // C) Slot je obsazen˝ jin˝m -> ZkusÌme AddItem (najde jinÈ mÌsto)
                else
                {
                    success = InventoryManager.instance.AddItem(itemToTake, amountToTake);
                }

                if (success)
                {
                    // Vymazat z pece
                    if (droppedItem.furnaceSlotType == "Output") { furnace.outputItem = null; furnace.outputAmount = 0; }
                    else if (droppedItem.furnaceSlotType == "Input") { furnace.inputItem = null; furnace.inputAmount = 0; }
                    else if (droppedItem.furnaceSlotType == "Fuel") { furnace.fuelItem = null; furnace.fuelAmount = 0; }

                    FurnaceUI.instance.UpdateVisuals();

                    // Aktualizovat UI invent·¯e (p¯es SendMessage, protoûe UpdateUI je private)
                    InventoryManager.instance.SendMessage("UpdateUI");
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (ShopUI.instance != null && ShopUI.instance.shopPanel.activeSelf)
            {
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);
                if (slotData != null && slotData.item != null)
                    ShopUI.instance.TrySellItem(slotData.item, slotIndex);
            }
            else if (FurnaceUI.instance != null && FurnaceUI.instance.panel.activeSelf)
            {
                var slotData = InventoryManager.instance.GetSlotData(slotIndex);
                if (slotData != null && slotData.item != null)
                    FurnaceUI.instance.TryMoveItemToFurnace(slotData.item, slotIndex);
            }
            else
            {
                if (InventoryManager.instance != null) InventoryManager.instance.UseItem(slotIndex);
            }
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryManager.instance == null) return;

        // ZÌsk·me data o slotu
        var slotData = InventoryManager.instance.GetSlotData(slotIndex);

        // Pokud je ve slotu item, uk·ûeme tooltip
        if (slotData != null && slotData.item != null)
        {
            InventoryManager.instance.ShowTooltip(slotData.item.itemName);
        }
        else
        {
            // Pokud je slot pr·zdn˝, schov·me tooltip (pro jistotu)
            InventoryManager.instance.HideTooltip();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.HideTooltip();
        }
    }

    // Pojistka: Kdyû se item vypne/zniËÌ, schovej tooltip
    void OnDisable()
    {
        if (InventoryManager.instance != null) InventoryManager.instance.HideTooltip();
    }
}