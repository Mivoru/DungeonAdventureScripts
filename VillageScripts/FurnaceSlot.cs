using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FurnaceSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Type")]
    public string slotType = "Input"; // "Input", "Fuel" nebo "Output"

    private GameObject draggingObject;

    // --- 1. PØÍJEM (Drop z Inventáøe DO Pece) ---
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            if (draggedItem != null && InventoryManager.instance != null && draggedItem.parentSlot != null)
            {
                var slotData = InventoryManager.instance.GetSlotData(draggedItem.parentSlot.slotIndex);

                if (slotData != null && slotData.item != null)
                {
                    if (FurnaceUI.instance != null)
                    {
                        FurnaceUI.instance.HandleItemDrop(slotData.item, draggedItem.parentSlot.slotIndex, slotData.amount, slotType);
                    }
                }
            }
        }
    }

    // --- 2. ZAÈÁTEK TAŽENÍ (Z Pece VEN) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (FurnaceUI.instance == null || FurnaceUI.instance.currentFurnace == null) return;

        ItemData item = null;
        int amount = 0;
        var furnace = FurnaceUI.instance.currentFurnace;

        // Zjistíme, co je ve slotu
        if (slotType == "Input") { item = furnace.inputItem; amount = furnace.inputAmount; }
        else if (slotType == "Fuel") { item = furnace.fuelItem; amount = furnace.fuelAmount; }
        else if (slotType == "Output") { item = furnace.outputItem; amount = furnace.outputAmount; }

        if (item == null || amount <= 0) return;

        // Vytvoøíme DUCHA (ikonu pod myší)
        draggingObject = new GameObject("DraggingFurnaceIcon");
        draggingObject.transform.SetParent(FurnaceUI.instance.transform.root); // Canvas
        draggingObject.transform.SetAsLastSibling();
        draggingObject.transform.position = eventData.position;

        // Obrázek
        Image img = draggingObject.AddComponent<Image>();
        img.sprite = item.icon;
        img.raycastTarget = false; // DÙLEŽITÉ: Aby neblokoval raycast dolù

        // Pøidáme DraggableItem skript
        DraggableItem dragScript = draggingObject.AddComponent<DraggableItem>();
        dragScript.isFromFurnace = true;
        dragScript.furnaceSlotType = slotType;
        dragScript.furnaceSource = furnace;

        // POJISTKA: DraggableItem pøidává CanvasGroup, musíme ho odblokovat
        CanvasGroup cg = draggingObject.GetComponent<CanvasGroup>();
        if (cg != null) cg.blocksRaycasts = false;

        DraggableItem.itemBeingDragged = dragScript;
    }

    // --- 3. PRÙBÌH ---
    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObject != null)
        {
            draggingObject.transform.position = eventData.position;
        }
    }

    // --- 4. KONEC ---
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggingObject != null)
        {
            Destroy(draggingObject);
        }
        DraggableItem.itemBeingDragged = null;
    }
}