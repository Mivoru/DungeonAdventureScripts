using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float pickupRange = 1.5f;
    public LayerMask lootLayer;

    // --- PØIDANÁ ÈÁST: INICIALIZACE ---
    void Start()
    {
        PlayerInput input = GetComponent<PlayerInput>();
        if (input != null)
        {
            // Ujistíme se, že mapa "Player" je aktivní
            // Pokud se tvá mapa jmenuje "Gameplay", pøepiš to na "Gameplay"
            InputActionMap map = input.actions.FindActionMap("Player");
            if (map != null)
            {
                map.Enable();
                // Povolíme konkrétnì i akci Interact, pro jistotu
                InputAction interactAction = map.FindAction("Interact");
                if (interactAction != null) interactAction.Enable();
            }
        }
    }
    // ----------------------------------

    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log($"Input Interact: {context.phase}");
        // 1. Test tlaèítka
        if (context.performed)
        {
            // Debug.Log(" Tlaèítko E stisknuto!");

            // 2. Test fyziky - Hledáme VŠE v dosahu (bez filtru vrstvy, aby to našlo i Portál)
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRange);
            foreach (Collider2D hit in hits)
            {
                // A. Zkusíme najít LOOT
                LootPickup item = hit.GetComponent<LootPickup>();
                if (item != null && item.canBePickedUp)
                {
                    // Debug.Log(" -> Sbírám loot!");
                    item.Collect();
                    return; // Sebrali jsme, konèíme (nepokraèujeme k portálu)
                }

                // B. Zkusíme najít PORTÁL
                VillagePortal portal = hit.GetComponent<VillagePortal>();
                if (portal != null)
                {
                    // Debug.Log(" -> Aktivuji portál!");
                    portal.Interact();
                    return; // Aktivovali jsme, konèíme
                }
                // 3. DUNGEON EXIT (Výstup z dungeonu - NOVÉ)
                DungeonExit dExit = hit.GetComponent<DungeonExit>();
                if (dExit != null)
                {
                    dExit.Interact();
                    return;
                }
                // NOVÉ: SHOPKEEPER
                Shopkeeper shop = hit.GetComponent<Shopkeeper>();
                if (shop != null)
                {
                    shop.Interact();
                    return;
                }
            }
        }
    }
    public void OnInventoryAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Zavoláme manažera pøes Singleton
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.OnToggleInventory(context);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}