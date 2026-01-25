using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShearShadow : MonoBehaviour
{
    [Header("Materiál")]
    public Material skewMaterial; // Pøetáhni sem BuildingShadowMat

    [Header("Nastavení Èasu")]
    [Tooltip("Kdy se stín objeví (0.2 = cca 5:00 ráno)")]
    [Range(0, 1)] public float dayStartTime = 0.2f;

    [Tooltip("Kdy stín zmizí (0.958 = cca 23:00 veèer)")]
    [Range(0, 1)] public float dayEndTime = 0.958f;

    [Header("Vzhled")]
    public Vector3 offset = new Vector3(0, -0.1f, 0);
    [Range(0, 1)] public float alpha = 0.6f;

    [Header("Síla efektu")]
    public float maxSkew = 1.5f; // Jak moc se natáhne do stran

    private GameObject shadowObj;
    private SpriteRenderer mySr;
    private SpriteRenderer shadowSr;
    private Material myShadowMat;

    void Start()
    {
        mySr = GetComponent<SpriteRenderer>();

        // 1. Vytvoøení stínu
        shadowObj = new GameObject("Shadow_" + gameObject.name);
        shadowObj.transform.parent = transform;
        shadowObj.transform.localPosition = offset;
        shadowObj.transform.localScale = Vector3.one;

        // 2. Renderer
        shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = mySr.sprite;
        shadowSr.sortingLayerName = mySr.sortingLayerName;
        shadowSr.sortingOrder = mySr.sortingOrder - 1; // Pod domem

        // 3. Materiál
        if (skewMaterial != null)
        {
            shadowSr.material = new Material(skewMaterial);
            myShadowMat = shadowSr.material;
            myShadowMat.SetColor("_Color", new Color(0, 0, 0, alpha));
        }
    }

    void LateUpdate()
    {
        if (TimeManager.instance == null || myShadowMat == null) return;

        float time = TimeManager.instance.currentTime;

        // Podmínka: Èas musí být mezi Startem a Koncem
        if (time >= dayStartTime && time <= dayEndTime)
        {
            shadowSr.enabled = true;

            // Vypoèítáme "Progress" (0.0 na zaèátku dne, 1.0 na konci dne)
            float totalDuration = dayEndTime - dayStartTime;
            float dayProgress = (time - dayStartTime) / totalDuration;

            // Vypoèítáme Skew (Zkosení)
            // Ráno = -maxSkew (Doleva)
            // Veèer = maxSkew (Doprava)
            float currentSkew = Mathf.Lerp(-maxSkew, maxSkew, dayProgress);

            // Pošleme to do shaderu
            myShadowMat.SetFloat("_SkewX", currentSkew);

            // Bonus: Zmìna délky (v poledne kratší, ráno/veèer delší)
            // Použijeme Sinusovku pro plynulé zkrácení uprostøed intervalu
            float heightScale = 1f - (Mathf.Sin(dayProgress * Mathf.PI) * 0.4f);
            shadowObj.transform.localScale = new Vector3(1f, heightScale, 1f);

            // Optional: Fade Out ke konci (aby ve 23:00 nezmizel skokovì)
            float fadeEdge = 0.05f; // Posledních 5% èasu bude mizet
            float currentAlpha = alpha;

            if (dayProgress > (1.0f - fadeEdge))
            {
                // Pøepoèet posledního úseku na 1.0 -> 0.0
                float fadeProgress = (1.0f - dayProgress) / fadeEdge;
                currentAlpha = alpha * fadeProgress;
            }
            else if (dayProgress < fadeEdge) // Fade In ráno
            {
                float fadeProgress = dayProgress / fadeEdge;
                currentAlpha = alpha * fadeProgress;
            }

            myShadowMat.SetColor("_Color", new Color(0, 0, 0, currentAlpha));
        }
        else
        {
            shadowSr.enabled = false;
        }
    }
}