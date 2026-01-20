using UnityEngine;

public class PixelPerfectSmoothing : MonoBehaviour
{
    // Nastav podle velikosti svých pixelù (PPU - Pixels Per Unit)
    // Pokud máš importované sprity s PPU 16, napiš sem 16.
    public float pixelsPerUnit = 16f;

    private Transform parent;

    void Start()
    {
        // Pøedpokládáme, že tento skript je na "Grafice" (Child), 
        // která je podøazená "Fyzice" (Parent).
        if (transform.parent != null)
        {
            parent = transform.parent;
        }
    }

    void LateUpdate()
    {
        if (parent != null)
        {
            Vector3 newPos = parent.position;

            // Zaokrouhlení pozice na nejbližší "pixel" ve svìtì Unity
            newPos.x = (Mathf.Round(parent.position.x * pixelsPerUnit) / pixelsPerUnit);
            newPos.y = (Mathf.Round(parent.position.y * pixelsPerUnit) / pixelsPerUnit);

            transform.position = newPos;
        }
    }
}