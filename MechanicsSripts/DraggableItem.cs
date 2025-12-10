using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public InventorySlot parentSlot;

    // --- NOVÉ PROMÌNNÉ PRO PEC ---
    public bool isFromFurnace = false;
    public string furnaceSlotType;
    public FurnaceInteractable furnaceSource;
    // -----------------------------

    private Image image;
    private CanvasGroup canvasGroup;
    public static DraggableItem itemBeingDragged;

    void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null && !isFromFurnace) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (image.sprite == null || !image.enabled) return;

        itemBeingDragged = this;
        parentSlot = GetComponentInParent<InventorySlot>();
        originalParent = transform.parent;

        // Pøesuneme nad všechno ostatní (do koøene InventoryManageru nebo HUDu)
        // Používáme root.parent nebo pøímo transform InventoryManageru
        if (InventoryManager.instance != null)
        {
            transform.SetParent(InventoryManager.instance.transform);
        }
        else
        {
            // Fallback, kdyby manager nebyl (což by nemìlo nastat)
            transform.SetParent(transform.root);
        }

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (image.sprite == null || !image.enabled) return;

        // --- OPRAVA CHYBY ---
        // Místo Input.mousePosition použijeme pozici z eventu (funguje pro myš i dotyk)
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Pojistka, kdyby objekt zmizel bìhem dragu
        if (this == null || gameObject == null) return;
        if (image.sprite == null || !image.enabled) return;

        itemBeingDragged = null;

        // Pokud pouštíme nad UI (napø. jiný slot), OnDrop v tom slotu to vyøeší.
        // Pokud pouštíme MIMO UI (do svìta), vyhodíme item.
        if (eventData.pointerEnter == null || eventData.pointerEnter.layer != LayerMask.NameToLayer("UI"))
        {
            // Vyhození itemu
            if (InventoryManager.instance != null && parentSlot != null)
            {
                InventoryManager.instance.DropItem(parentSlot.slotIndex);
            }
        }

        // Pokud jsme item "nepustili" úspìšnì jinam (tzn. nebyl znièen/pøesunut), vrátíme ho domù
        FinishDrag();
    }

    public void FinishDrag()
    {
        if (this == null) return;
        if (isFromFurnace) return; // Pec si to øeší jinak (Destroy)

        if (canvasGroup) canvasGroup.blocksRaycasts = true;
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
        }
        if (itemBeingDragged == this) itemBeingDragged = null;
    }
}