using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform originalParent;
    [HideInInspector] public InventorySlot parentSlot;

    private Image image;
    private CanvasGroup canvasGroup;

    // STATICKÁ PROMÌNNÁ: Pamatuje si, co právì držíš myší
    public static DraggableItem itemBeingDragged;

    void Awake()
    {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (image.sprite == null || !image.enabled) return;

        // Uložíme si referenci na sebe
        itemBeingDragged = this;

        parentSlot = GetComponentInParent<InventorySlot>();
        originalParent = transform.parent;

        // Pøipojíme se k rootu (InventoryManageru), aby byla ikona nad vším
        transform.SetParent(InventoryManager.instance.transform);

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (image.sprite == null || !image.enabled) return;
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // POJISTKA: Pokud byl objekt znièen v OnDrop (pøi swapu), ukonèíme funkci
        if (this == null || gameObject == null) return;

        if (image.sprite == null || !image.enabled) return;

        // ... zbytek tvého kódu (FinishDrag / DropItem) ...

        // Tady by mìlo být volání FinishDrag() nebo logika pro návrat
        // Pokud jsi to tam mìl, nech to tam.
        // Dùležité je jen pøidat tu kontrolu na zaèátek.

        // Drop logic (vyhození na zem) - jen pokud stále existujeme
        if (!eventData.pointerEnter || eventData.pointerEnter.layer != LayerMask.NameToLayer("UI"))
        {
            InventoryManager.instance.DropItem(parentSlot.slotIndex);
        }
        else
        {
            // Pokud jsme pustili nad UI, ale ne nad slotem, vrátíme se
            FinishDrag();
        }
    }

    public void FinishDrag()
    {
        // Pojistka proti chybám
        if (this == null) return;

        // Zapneme znovu klikání
        if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

        // Vrátíme domù
        transform.SetParent(originalParent);
        transform.localPosition = Vector3.zero;

        // Vyèistíme statickou referenci
        if (itemBeingDragged == this) itemBeingDragged = null;
    }
}