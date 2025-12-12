using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Odkaz na slot, odkud item pochází (originál)
    [HideInInspector] public InventorySlot parentSlot;

    // --- PROMÌNNÉ PRO PEC ---
    public bool isFromFurnace = false;
    public string furnaceSlotType;
    public FurnaceInteractable furnaceSource;
    // -------------------------

    private Image image;
    private CanvasGroup canvasGroup;

    // Zmìna: Už ne static reference na tento skript, ale na skript na Duchovi
    public static DraggableItem itemBeingDragged;

    private GameObject draggingObject; // Náš "Duch"

    void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (parentSlot == null) parentSlot = GetComponentInParent<InventorySlot>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Kontrola: Máme co tahat?
        if (image.sprite == null || !image.enabled) return;

        // Zjistíme rodièovský slot (pokud ještì nemáme)
        if (parentSlot == null) parentSlot = GetComponentInParent<InventorySlot>();

        // 1. VYTVOØÍME DUCHA (Kopii ikony)
        draggingObject = new GameObject("GhostIcon");
        draggingObject.transform.SetParent(transform.root); // Hodíme ho pøímo do Canvasu (navrch)
        draggingObject.transform.SetAsLastSibling();

        // Nastavíme pozici na myš
        draggingObject.transform.position = eventData.position;

        // Pøidáme obrázek
        Image ghostImage = draggingObject.AddComponent<Image>();
        ghostImage.sprite = image.sprite;
        ghostImage.raycastTarget = false; // Aby neblokoval myš pøi dropu!

        // Pøidáme tento skript i na Ducha, aby ho pøijímající slot poznal
        DraggableItem ghostScript = draggingObject.AddComponent<DraggableItem>();
        ghostScript.parentSlot = this.parentSlot; // Pøedáme informaci, odkud jsme
        ghostScript.isFromFurnace = this.isFromFurnace;
        ghostScript.furnaceSlotType = this.furnaceSlotType;
        ghostScript.furnaceSource = this.furnaceSource;

        // Nastavíme globální referenci na DUCHA
        itemBeingDragged = ghostScript;

        // 2. SKRYJEME ORIGINÁL (Jen vizuálnì)
        if (canvasGroup != null) canvasGroup.alpha = 0.5f; // Zprùhledníme
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingObject != null)
        {
            draggingObject.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. UKLIDÍME DUCHA
        if (draggingObject != null)
        {
            Destroy(draggingObject);
        }

        // 2. OBNOVÍME ORIGINÁL
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        itemBeingDragged = null;

        // 3. LOGIKA VYHOZENÍ (Pokud jsme pustili mimo UI)
        if (eventData.pointerEnter == null || eventData.pointerEnter.layer != LayerMask.NameToLayer("UI"))
        {
            // Pokud to není z pece (z pece nevyhazujeme na zem)
            if (!isFromFurnace && InventoryManager.instance != null && parentSlot != null)
            {
                InventoryManager.instance.DropItem(parentSlot.slotIndex);
            }
        }
    }

    // Tuto metodu volá InventorySlot/Drop pøi úspìšném dropu
    // U Ducha už nic dìlat nemusíme (znièí se v OnEndDrag), ale musíme resetovat originál
    public void FinishDrag()
    {
        // Tato metoda byla dùležitá pro fyzický pøesun, teï už slouží jen jako pojistka
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
}