using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class FurnaceSlot : MonoBehaviour, IDropHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Slot Type")]
    // Napiš sem pøesnì "Input", "Fuel" nebo "Output"
    public string slotType = "Input";

    // Promìnná pro doèasnou iku pøi tažení
    private GameObject draggingObject;

    // --- 1. PØÍJEM PØEDMÌTU (Z INVENTÁØE DO PECE) ---
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
                    // Pošleme to do FurnaceUI (vezmeme celý stack = slotData.amount)
                    if (FurnaceUI.instance != null)
                    {
                        FurnaceUI.instance.HandleItemDrop(slotData.item, draggedItem.parentSlot.slotIndex, slotData.amount, slotType);
                    }
                }
            }
        }
    }

    // --- 2. ZAÈÁTEK TAŽENÍ (Z PECE VEN) ---
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Kontrola, jestli je pec otevøená a má data
        if (FurnaceUI.instance == null || FurnaceUI.instance.currentFurnace == null) return;

        ItemData item = null;
        int amount = 0;
        var furnace = FurnaceUI.instance.currentFurnace;

        // Zjistíme, co je v tomto slotu
        if (slotType == "Input") { item = furnace.inputItem; amount = furnace.inputAmount; }
        else if (slotType == "Fuel") { item = furnace.fuelItem; amount = furnace.fuelAmount; }
        else if (slotType == "Output") { item = furnace.outputItem; amount = furnace.outputAmount; }

        // Pokud je slot prázdný, nic netaháme
        if (item == null || amount <= 0) return;

        // Vytvoøíme doèasnou ikonku (Ghost Icon)
        draggingObject = new GameObject("DraggingFurnaceIcon");
        draggingObject.transform.SetParent(FurnaceUI.instance.transform.root); // Dáme to úplnì navrch (Canvas)
        draggingObject.transform.SetAsLastSibling();

        // Pøidáme obrázek
        Image img = draggingObject.AddComponent<Image>();
        img.sprite = item.icon;
        img.raycastTarget = false; // Aby neblokovala myš

        // Pøidáme skript DraggableItem, aby to InventorySlot poznal
        DraggableItem dragScript = draggingObject.AddComponent<DraggableItem>();
        dragScript.isFromFurnace = true;
        dragScript.furnaceSlotType = slotType;
        dragScript.furnaceSource = furnace;

        // Nastavíme globální referenci
        DraggableItem.itemBeingDragged = dragScript;
    }

    // --- 3. PRÙBÌH TAŽENÍ ---
    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObject != null)
        {
            draggingObject.transform.position = eventData.position;
        }
    }

    // --- 4. KONEC TAŽENÍ ---
    public void OnEndDrag(PointerEventData eventData)
    {
        // Pokud jsme to pustili nad Inventáøem, InventorySlot.OnDrop si to pøevezme a zpracuje.
        // My jen uklidíme ten doèasný obrázek.

        if (draggingObject != null)
        {
            Destroy(draggingObject);
        }
        DraggableItem.itemBeingDragged = null;
    }
}