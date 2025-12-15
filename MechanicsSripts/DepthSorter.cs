using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DepthSorter : MonoBehaviour
{
    [Header("Settings")]
    public bool isStatic = false; // Zaškrtni pro stromy/kameny (ušetøí vıkon)
    public float offset = 0f;     // Doladìní (kdyby se to pøekrıvalo špatnì)

    // Èím vyšší èíslo, tím pøesnìjší tøídìní (100 je standard)
    private const int SORTING_ORDER_MULTIPLIER = 100;

    private SpriteRenderer sr;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        // Pro statické objekty (stromy) nastavíme jednou a hotovo
        if (isStatic)
        {
            UpdateSortingOrder();
            enabled = false; // Vypneme Update, a to neere vıkon
        }
    }

    void Update()
    {
        // Pro pohyblivé objekty (hráè, monstra) aktualizujeme kadı snímek
        UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        if (sr == null) return;

        // Vypoèítáme Y pozici spodní hrany spritu (nohy)
        // sr.bounds.min.y nám dá pøesnì spodní okraj obrázku ve svìtì
        float bottomY = sr.bounds.min.y + offset;

        // Vzorec: Èím niší Y (dole na obrazovce), tím vyšší Order (vykreslí se pøes ostatní)
        sr.sortingOrder = -(int)(bottomY * SORTING_ORDER_MULTIPLIER);
    }
}