using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SceneLightControl : MonoBehaviour
{
    private Light2D myLight;

    [Header("Dungeon Settings")]
    public float dungeonRadius = 8f;   // Velká aura
    public float dungeonIntensity = 1.0f;

    void Start()
    {
        myLight = GetComponent<Light2D>();
        CheckScene();
    }

    // Volat i pøi zmìnì scény (pokud hráè neumírá/nenièí se)
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        myLight = GetComponent<Light2D>(); // Znovu najít pro jistotu
        CheckScene();
    }

    void CheckScene()
    {
        if (myLight == null) return;

        string scene = SceneManager.GetActiveScene().name;

        if (scene == "DungeonScene")
        {
            // --- NASTAVENÍ PRO DUNGEON ---
            myLight.enabled = true;
            myLight.pointLightOuterRadius = dungeonRadius; // Vìtší dosah
            myLight.intensity = dungeonIntensity;
            myLight.color = new Color(1f, 0.9f, 0.7f); // Teplá barva pochodnì
        }
        else if (scene == "VillageScene")
        {
            // Ve vesnici svìtlo vypneme (svítí slunce)
            // NEBO ho necháme zapnuté jen v noci (pokud chceš extra detail)
            myLight.enabled = false;
        }
    }
}