using UnityEngine;
using UnityEngine.Rendering; // Potøeba pro SortingGroup (volitelné)

public class UniversalDepthSorter : MonoBehaviour
{
    [Header("Settings")]
    public bool isStatic = false;
    public float offset = 0f;
    private const int SORTING_ORDER_MULTIPLIER = 100;

    private Renderer rend; // Bere SpriteRenderer I TilemapRenderer

    void Start()
    {
        rend = GetComponent<Renderer>(); // Najde jakýkoliv renderer

        if (isStatic)
        {
            UpdateSortingOrder();
            enabled = false;
        }
    }

    void Update()
    {
        UpdateSortingOrder();
    }

    void UpdateSortingOrder()
    {
        if (rend == null) return;

        // Renderer.bounds funguje pro oba typy stejnì
        float bottomY = rend.bounds.min.y + offset;
        rend.sortingOrder = -(int)(bottomY * SORTING_ORDER_MULTIPLIER);
    }
}