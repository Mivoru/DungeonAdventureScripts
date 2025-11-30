using UnityEngine;
using System.Collections;

public class LootPickup : MonoBehaviour
{
    public ItemData itemData;
    public int amount = 1;

    public bool canBePickedUp = false;
    private SpriteRenderer spriteRenderer;
    private Collider2D col; // Odkaz na collider

    [Header("Settings")]
    public float pickupDelay = 1.0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Na zaèátku vypneme collider, aby to nešlo sebrat hned (volitelné)
        // Ale pro E-interakci to nevadí, dùležitá je promìnná canBePickedUp
        canBePickedUp = false;

        if (itemData != null && itemData.icon != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }

        StartCoroutine(AnimateDrop());
    }

    public void SetItem(ItemData newItem, int count = 1)
    {
        itemData = newItem;
        amount = count;
        if (spriteRenderer != null && newItem.icon != null)
        {
            spriteRenderer.sprite = newItem.icon;
        }
    }

    IEnumerator AnimateDrop()
    {
        Vector3 startPos = transform.position;

        // Náhodný posun do strany (výskok)
        // Používáme UnityEngine.Random, aby to neházelo chybu
        Vector3 randomOffset = (Vector3)UnityEngine.Random.insideUnitCircle * 0.5f;
        Vector3 targetPos = startPos + randomOffset;

        float timer = 0f;
        float duration = 0.5f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // Plynulý pohyb
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Výška výskoku (parabola)
            currentPos.y += Mathf.Sin(t * Mathf.PI) * 0.5f;

            transform.position = currentPos;
            yield return null;
        }

        // Èekáme zbytek èasu do povolení sbìru
        float remainingDelay = pickupDelay - duration;
        if (remainingDelay > 0) yield return new WaitForSeconds(remainingDelay);

        canBePickedUp = true; // TEÏ jde sebrat klávesou E
        // Debug.Log("Item je pøipraven k sebrání!");
    }

    // Tuto metodu volá PlayerInteraction, když zmáèkneš E
    public void Collect()
    {
        if (itemData != null)
        {
            // 1. Zkusíme pøidat do inventáøe
            bool added = InventoryManager.instance.AddItem(itemData, amount);

            // 2. POKUD se to podaøilo pøidat (added == true)
            if (added)
            {
                // Log do konzole/UI
                LootLogManager.instance?.AddLog($"{itemData.itemName} x{amount}", itemData.icon);

                // 3. ZNIÈÍME OBJEKT NA ZEMI
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("Inventáø je plný! Nemùžu sebrat.");
            }
        }
    }
}