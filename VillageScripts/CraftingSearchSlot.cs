using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingSearchSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            // Item musí být z inventáøe (ne z pece) a nesmí to být "Duch"
            if (draggedItem != null && InventoryManager.instance != null && !draggedItem.isFromFurnace)
            {
                var slotData = InventoryManager.instance.GetSlotData(draggedItem.parentSlot.slotIndex);

                if (slotData != null && slotData.item != null)
                {
                    // Nastavíme filtr v CraftingUI
                    if (CraftingUI.instance != null)
                    {
                        CraftingUI.instance.SetSearchItem(slotData.item);
                    }
                }
            }
        }
    }

    // Kliknutím filtr vymažeme
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CraftingUI.instance != null)
        {
            CraftingUI.instance.SetSearchItem(null);
        }
    }
}