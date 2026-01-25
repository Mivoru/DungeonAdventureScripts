using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class AutoBlobShadow : MonoBehaviour
{
    [Header("Nastavení")]
    [Tooltip("Pokud je prázdné, naète se automaticky 'ShadowBlob' z Resources")]
    public Sprite ShadowBlob;

    [Tooltip("Šíøka stínu vùèi postavì (1.0 = stejnì široký jako postava)")]
    public float widthMultiplier = 0.8f;

    [Tooltip("Výška (zploštìní) stínu. Menší èíslo = placatìjší.")]
    public float heightMultiplier = 0.3f;

    [Tooltip("Prùhlednost stínu (0 až 1)")]
    [Range(0, 1)] public float opacity = 0.5f;

    [Header("Sorting (Vrstvy)")]
    [Tooltip("Napiš pøesný název vrstvy, kam stín patøí (napø. Ground).")]
    public string shadowLayerName = "Ground";

    [Tooltip("Poøadí ve vrstvì. Pokud má podlaha 0, dej sem tøeba 1, aby byl stín nad ní.")]
    public int shadowOrder = 0;

    [Header("Pozice")]
    public Vector3 manualOffset = Vector3.zero;

    // --- INTERNÍ PROMÌNNÉ ---
    private GameObject shadowObj;
    private float distToFeet;

    void Start()
    {
        SpriteRenderer parentSr = GetComponent<SpriteRenderer>();
        if (ShadowBlob == null) ShadowBlob = Resources.Load<Sprite>("ShadowBlob");

        if (parentSr == null || ShadowBlob == null) return;

        // 1. Zmìøíme vzdálenost k nohám
        distToFeet = parentSr.bounds.extents.y;

        // 2. Vytvoøení objektu stínu
        shadowObj = new GameObject("Shadow_Stable");
        shadowObj.transform.parent = transform;

        // 3. Renderer a Nastavení Vrstvy
        SpriteRenderer shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = ShadowBlob;
        shadowSr.color = new Color(0, 0, 0, opacity);

        // --- ZMÌNA ZDE ---
        // Nastavíme vrstvu podle promìnné v Inspectoru (Ground)
        shadowSr.sortingLayerName = shadowLayerName;
        // Nastavíme poøadí natvrdo (aby se nebilo s podlahou)
        shadowSr.sortingOrder = shadowOrder;
        // -----------------

        // 4. Scale (Velikost)
        float parentWidth = parentSr.bounds.size.x;
        float spriteSize = ShadowBlob.bounds.size.x;
        float finalScale = (parentWidth * widthMultiplier) / spriteSize;
        shadowObj.transform.localScale = new Vector3(finalScale, finalScale * heightMultiplier, 1f);
    }

    void LateUpdate()
    {
        if (shadowObj != null)
        {
            // 1. Reset Rotace
            shadowObj.transform.rotation = Quaternion.identity;

            // 2. Fixní Pozice
            Vector3 anchorPos = transform.position;
            anchorPos.y -= distToFeet;
            shadowObj.transform.position = anchorPos + manualOffset;
        }
    }
}