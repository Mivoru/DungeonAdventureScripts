using UnityEngine;

public class VillagePortal : MonoBehaviour
{
    [Header("References")]
    public GameObject dungeonMenuPanel; // Odkaz na UI Panel

    void Start()
    {
        // AUTOMATICKÉ HLEDÁNÍ
        if (dungeonMenuPanel == null)
        {
            // 1. Najdeme skript DungeonMenuUI ve scénì (i když je objekt vypnutý!)
            DungeonMenuUI menuScript = FindFirstObjectByType<DungeonMenuUI>(FindObjectsInactive.Include);

            // 2. Pokud ho najdeme, získáme jeho GameObject
            if (menuScript != null)
            {
                dungeonMenuPanel = menuScript.gameObject;
                Debug.Log(" VillagePortal: Automaticky jsem našel DungeonMenu.");
            }
            else
            {
                Debug.LogError(" VillagePortal: Nemùžu najít DungeonMenuUI! Ujisti se, že je ve scénì.");
            }
        }
    }

    // Tuto metodu zavolá PlayerInteraction, když u portálu zmáèkneš E
    public void Interact()
    {
        if (dungeonMenuPanel != null)
        {
            bool isActive = dungeonMenuPanel.activeSelf;
            dungeonMenuPanel.SetActive(!isActive); // Zapne/Vypne menu
        }
        else
        {
            Debug.LogError("Chybí odkaz na DungeonMenu!");
        }
    }
}