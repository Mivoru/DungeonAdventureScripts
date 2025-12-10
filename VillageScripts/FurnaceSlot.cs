using UnityEngine;
using UnityEngine.EventSystems;

public class FurnaceSlot : MonoBehaviour, IDropHandler
{
    [Header("Slot Type")]
    // Napiš sem pøesnì "Input" pro horní slot nebo "Fuel" pro spodní slot
    public string slotType = "Input";

    public void OnDrop(PointerEventData eventData)
    {
        // Zjistíme, co nám sem spadlo
        if (eventData.pointerDrag != null)
        {
            DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            if (draggedItem != null && InventoryManager.instance != null)
            {
                // Získáme data o pøedmìtu z pùvodního slotu
                var slotData = InventoryManager.instance.GetSlotData(draggedItem.parentSlot.slotIndex);

                if (slotData != null && slotData.item != null)
                {
                    // Pošleme to do FurnaceUI na zpracování
                    if (FurnaceUI.instance != null)
                    {
                        // ZDE JE TA ZMÌNA: Pøidali jsme 'slotData.amount' jako 3. parametr
                        FurnaceUI.instance.HandleItemDrop(slotData.item, draggedItem.parentSlot.slotIndex, slotData.amount, slotType);
                    }
                }
            }
        }
    }
}