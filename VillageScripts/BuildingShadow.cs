using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(SpriteRenderer))]
public class BuildingShadow : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 offset = new Vector3(0, -0.5f, 0); // Posun pod dùm
    public float maxTiltAngle = 60f; // Maximální náklon stínu (napø. -60 až +60)
    [Range(0, 1)] public float alpha = 0.6f;

    // Odkaz na cyklus (musíš mít pøístup k èasu)
    // Pøedpokládám, že TimeManager vrací 0.0 až 1.0

    private GameObject shadowObj;
    private SpriteRenderer mySr;
    private SpriteRenderer shadowSr;

    void Start()
    {
        mySr = GetComponent<SpriteRenderer>();

        shadowObj = new GameObject("Shadow_" + gameObject.name);
        shadowObj.transform.parent = transform;
        shadowObj.transform.localPosition = offset;
        shadowObj.transform.localScale = Vector3.one;

        shadowSr = shadowObj.AddComponent<SpriteRenderer>();
        shadowSr.sprite = mySr.sprite;
        shadowSr.sortingLayerName = mySr.sortingLayerName;

        // DÙLEŽITÉ: Stín budovy musí být VŽDY pod budovou (-1)
        shadowSr.sortingOrder = mySr.sortingOrder - 1;
        shadowSr.color = new Color(0, 0, 0, alpha);

        // Statické budovy se nepøeklápí
        shadowSr.flipX = false;
    }

    void LateUpdate()
    {
        if (TimeManager.instance == null) return;

        float time = TimeManager.instance.currentTime;

        // Logika: Stín je vidìt jen ve dne (napø. 0.2 až 0.8)
        if (time >= 0.2f && time <= 0.8f)
        {
            shadowSr.enabled = true;

            // Pøevedeme èas dne na rozsah 0.0 až 1.0 (kde 0.5 je pravé poledne)
            float dayProgress = (time - 0.2f) / 0.6f;

            // Interpolace úhlu: Ráno maxTilt, Veèer -maxTilt
            float angle = Mathf.Lerp(maxTiltAngle, -maxTiltAngle, dayProgress);

            // Délka stínu: Ráno/Veèer dlouhý (1.5x), v poledne krátký (0.5x)
            // Vypoèítáme vzdálenost od poledne (0.5)
            float distFromNoon = Mathf.Abs(dayProgress - 0.5f);
            float stretch = 0.5f + (distFromNoon * 2.0f); // Výsledek: 0.5 až 1.5

            // Aplikace:
            // Rotace: Osa X = -60 (leží), Z = Náš vypoèítaný úhel
            shadowObj.transform.rotation = Quaternion.Euler(-60f, 0f, angle);
            shadowObj.transform.localScale = new Vector3(1f, stretch, 1f);

            // Barva (Fade in/out pøi svítání/stmívání)
            shadowSr.color = new Color(0, 0, 0, alpha);
        }
        else
        {
            shadowSr.enabled = false; // V noci stín není
        }
    }
}