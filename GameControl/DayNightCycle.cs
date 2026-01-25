using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("Propojení")]
    public Transform player;
    public Transform sunSystem;
    public Light2D globalLight;

    [Header("Dvojité Slunce (Stejný smìr)")]
    public Light2D hardShadowSun;   // 1. Svìtlo: Dìlá stíny (krátký radius, ostré)
    public Light2D softFillSun;     // 2. Svìtlo: Nedìlá stíny (velký radius, mìkké)

    [Header("Nastavení")]
    public float sunDistance = 20f; // Jak daleko obíhají

    [Header("Barvy a Èas")]
    public Gradient ambientColor;   // Barva okolí (Global Light)
    public Gradient sunColor;       // Barva obou sluncí

    void Update()
    {
        if (TimeManager.instance == null) return;

        float time = TimeManager.instance.currentTime;

        // --- 1. POHYB ---
        if (player != null && sunSystem != null)
        {
            sunSystem.position = player.position;
        }

        // --- 2. ROTACE ---
        if (sunSystem != null)
        {
            float angle = (time * 360f) - 90f;
            sunSystem.rotation = Quaternion.Euler(0, 0, angle);
        }

        // --- 3. POZICE A BARVA SVÌTEL ---
        // Vypoèítáme pozici jednou, platí pro obì svìtla
        Vector3 sunPos = new Vector3(sunDistance, 0, 0);
        Color currentSunColor = sunColor.Evaluate(time);

        // A) Hard Sun (Stíny)
        if (hardShadowSun != null)
        {
            hardShadowSun.transform.localPosition = sunPos;
            hardShadowSun.color = currentSunColor;
        }

        // B) Soft Sun (Fill - bez stínù)
        if (softFillSun != null)
        {
            // Úplnì stejná pozice jako Hard Sun
            softFillSun.transform.localPosition = sunPos;
            // Úplnì stejná barva
            softFillSun.color = currentSunColor;
        }

        // --- 4. GLOBAL LIGHT ---
        if (globalLight != null)
        {
            globalLight.color = ambientColor.Evaluate(time);
        }
    }
}