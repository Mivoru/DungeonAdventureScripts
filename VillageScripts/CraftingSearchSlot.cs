using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CraftingSearchSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Header("UI References")]
    // Ujisti se, že toto odkazuje na PRÁZDNÝ Image (Child objekt), ne na pozadí slotu!
    public Image icon;

    private ItemData currentItem;

    void OnEnable()
    {
        // Pøi naètení scény nebo zapnutí okna se slot vyèistí
        ClearSlot();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            DraggableItem draggedItem = eventData.pointerDrag.GetComponent<DraggableItem>();

            // Kontrola: Item musí být z inventáøe a musí být Material
            if (draggedItem != null && InventoryManager.instance != null && !draggedItem.isFromFurnace)
            {
                var slotData = InventoryManager.instance.GetSlotData(draggedItem.parentSlot.slotIndex);

                if (slotData != null && slotData.item != null)
                {
                    // 1. Nastavíme vizuál (ikonku)
                    SetVisualItem(slotData.item);

                    // 2. Aplikujeme filtr v CraftingUI
                    if (CraftingUI.instance != null)
                    {
                        CraftingUI.instance.SetSearchItem(slotData.item);
                    }
                }
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Kliknutím (levým/pravým) filtr zrušíme
        ClearSlot();

        if (CraftingUI.instance != null)
        {
            CraftingUI.instance.SetSearchItem(null);
        }
    }

    // --- ZDE JE HLAVNÍ OPRAVA BAREV ---
    void SetVisualItem(ItemData item)
    {
        currentItem = item;
        if (icon != null)
        {
            icon.sprite = item.icon;

            // DÙLEŽITÉ: Vynutíme bílou barvu (plná viditelnost, žádný tint)
            // Tím opravíme "èerný" item po návratu z dungeonu
            icon.color = Color.white;

            icon.enabled = true;
        }
    }

    void ClearSlot()
    {
        currentItem = null;
        if (icon != null)
        {
            icon.sprite = null;

            // Když je slot prázdný, nastavíme prùhlednou barvu a vypneme ho
            icon.color = Color.clear;
            icon.enabled = false;
        }
    }
}