using UnityEngine;
using UnityEngine.Rendering.Universal; // Pro praci se svetly

public class DayNightCycle : MonoBehaviour
{
    [Header("Propojení")]
    public Transform player;        // Sem pøetáhni Hráèe
    public Transform sunSystem;     // Sem pøetáhni ten prázdný objekt "SunSystem"
    public Light2D sunLight;        // Sem pøetáhni Spot Light (Slunce)
    public Light2D globalLight;     // Sem pøetáhni Global Light

    [Header("Nastavení Slunce")]
    public float sunDistance = 20f; // Jak daleko je slunce od hráèe

    [Header("Barvy a Èas")]
    public Gradient ambientColor;   // Barva dne (Global Light)
    public Gradient sunColor;       // Barva slunce

    void Update()
    {
        if (TimeManager.instance == null) return;

        // Získání èasu (0.0 až 1.0)
        float time = TimeManager.instance.currentTime;

        // --- 1. POHYB: Slunce pronásleduje hráèe ---
        if (player != null && sunSystem != null)
        {
            // Pivot se drží pøesnì na pozici hráèe
            sunSystem.position = player.position;
        }

        // --- 2. ROTACE: Slunce obíhá kolem hráèe ---
        if (sunSystem != null)
        {
            // Pøevod èasu na úhel (Ráno -90, Poledne 0, Veèer 90)
            // Celý kruh je 360 stupòù
            float angle = (time * 360f) - 90f;

            // Otoèíme celým systémem
            sunSystem.rotation = Quaternion.Euler(0, 0, angle);
        }

        // --- 3. VZDÁLENOST: Nastavení vzdálenosti slunce ---
        // Slunce (dítì) posuneme lokálnì po ose X nebo Y, aby kroužilo
        if (sunLight != null)
        {
            sunLight.transform.localPosition = new Vector3(sunDistance, 0, 0);
        }

        // --- 4. BARVY: Zmìna barev podle èasu ---
        if (globalLight != null) globalLight.color = ambientColor.Evaluate(time);
        if (sunLight != null) sunLight.color = sunColor.Evaluate(time);
    }
}