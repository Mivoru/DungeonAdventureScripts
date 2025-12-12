using UnityEngine;

public class CraftingTableInteractable : MonoBehaviour
{
    public float interactionRange = 3f;
    private CraftingUI ui;

    void Start()
    {
        // Najdeme UI (bezpeènì i když je vypnuté)
        ui = FindFirstObjectByType<CraftingUI>(FindObjectsInactive.Include);
    }

    void Update()
    {
        // Auto-Close pøi vzdálení
        if (ui != null && ui.gameObject.activeSelf && ui.currentTable == this)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && Vector2.Distance(transform.position, player.transform.position) > interactionRange)
            {
                ui.CloseCrafting();
            }
        }
    }

    public void Interact()
    {
        if (ui != null) ui.OpenCrafting(this);
    }
}