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
        // 1. Získáme data z pece
        if (FurnaceUI.instance == null || FurnaceUI.instance.currentFurnace == null) return;

        ItemData item = null;
        int amount = 0;
        var furnace = FurnaceUI.instance.currentFurnace;

        if (slotType == "Input") { item = furnace.inputItem; amount = furnace.inputAmount; }
        else if (slotType == "Fuel") { item = furnace.fuelItem; amount = furnace.fuelAmount; }
        else if (slotType == "Output") { item = furnace.outputItem; amount = furnace.outputAmount; }

        if (item == null || amount <= 0) return;

        // 2. Vytvoøíme DUCHA
        draggingObject = new GameObject("DraggingFurnaceIcon");
        draggingObject.transform.SetParent(FurnaceUI.instance.transform.root); // Canvas root
        draggingObject.transform.SetAsLastSibling(); // Úplnì navrch

        // Nastavíme pozici na myš
        draggingObject.transform.position = eventData.position;

        // Pøidáme obrázek
        Image img = draggingObject.AddComponent<Image>();
        img.sprite = item.icon;
        img.raycastTarget = false; // <--- DUCH MUSÍ BÝT PRÙHLEDNÝ PRO MYŠ

        // Pøidáme DraggableItem (aby InventorySlot poznal, že to je item)
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