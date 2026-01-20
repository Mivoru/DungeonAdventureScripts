using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class SimpleShadow : MonoBehaviour
{
    [Header("--- KALIBRACE (Ladìní) ---")]
    public bool flipDirection180 = false;
    public bool mirrorSprite = true;
    [Tooltip("Pokud máš pivot uprostøed, toto automaticky najde nohy.")]
    public bool fixCenterPivot = true;

    [Header("Manuální Posun")]
    [Tooltip("Zde si dolaï pozici stínu (X, Y). Z-osa urèuje poøadí.")]
    public Vector3 manualOffset = new Vector3(0f, -1f, 0.1f); // Výchozí drobné posunutí

    [Header("Typ Objektu")]
    public bool isStaticObject = false;

    [Header("Nastavení Vzhledu")]
    [Range(0, 1)] public float alpha = 0.6f;
    public float groundAngle = -60f;
    public float stretchStrength = 1.0f;

    [Header("Svìtlo")]
    public float lightRange = 12f;

    // Interní promìnné
    private Transform lightSource;
    private GameObject shadowObj;
    private SpriteRenderer mySr;
    private SpriteRenderer shadowSr;
    private bool isSun = false;
    private float baseRotationOffset = -90f;

    void Start()
    {
        mySr = GetComponent<SpriteRenderer>();
        FindLightSource();

        // Vytvoøení stínu
        shadowObj = new GameObject("Shadow_of_" + gameObject.name);
        shadowObj.transform.parent = transform;
        shadowObj.transform.localScale = Vector3.one;

        // Renderer
        shadowSr = shadowObj.AddComponent<SpriteRenderer>();

        // Sorting
        shadowSr.sortingLayerName = mySr.sortingLayerName;
        shadowSr.sortingOrder = mySr.sortingOrder - 1;
        shadowSr.color = new Color(0, 0, 0, alpha);
    }

    void LateUpdate()
    {
        if (shadowSr == null || mySr == null) return;
        if (lightSource == null) FindLightSource();

        // 1. Aktualizace pozice (OFFSET FIX)
        // Musíme to dìlat každým snímkem, aby fungovalo ladìní v Inspectoru
        Vector3 autoOffset = Vector3.zero;

        if (fixCenterPivot && mySr.sprite != null)
        {
            // Automaticky zjistíme, kde jsou nohy (polovina výšky spritu)
            float footDist = mySr.bounds.extents.y;
            autoOffset = new Vector3(0, -footDist, 0);
        }

        // Seèteme automatickou opravu + tvoje manuální nastavení
        shadowObj.transform.localPosition = autoOffset + manualOffset;


        // 2. Kopírování obrázku a Flip
        shadowSr.sprite = mySr.sprite;
        bool originalFlip = mySr.flipX;

        if (isStaticObject) shadowSr.flipX = mirrorSprite;
        else shadowSr.flipX = mirrorSprite ? !originalFlip : originalFlip;

        // 3. Svìtlo a Rotace
        if (lightSource != null)
        {
            Vector3 direction = transform.position - lightSource.position;
            float dist = direction.magnitude;

            if (!isSun)
            {
                if (dist > lightRange)
                {
                    shadowSr.enabled = false;
                    return;
                }
                shadowSr.enabled = true;
                float fade = 1f - (dist / lightRange);
                shadowSr.color = new Color(0, 0, 0, alpha * fade);
            }
            else
            {
                shadowSr.enabled = true;
                shadowSr.color = new Color(0, 0, 0, alpha);
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float totalRotation = angle + baseRotationOffset;
            if (flipDirection180) totalRotation += 180f;

            shadowObj.transform.rotation = Quaternion.Euler(groundAngle, 0f, totalRotation);

            float stretch = isSun ? 1.2f : (1f + (stretchStrength * 2f / (dist + 0.1f)));
            shadowObj.transform.localScale = new Vector3(1f, Mathf.Clamp(stretch, 1f, 3f), 1f);
        }
    }

    void FindLightSource()
    {
        GameObject sunObj = GameObject.FindGameObjectWithTag("Sun");
        if (sunObj != null)
        {
            lightSource = sunObj.transform;
            isSun = true;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                lightSource = playerObj.transform;
                isSun = false;
            }
        }
    }
}