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
        // 1. Test tlaèítka
        if (context.performed)
        {
            Debug.Log(" Tlaèítko E stisknuto! Volám fyziku...");

            // 2. Test fyziky
            Collider2D[] loots = Physics2D.OverlapCircleAll(transform.position, pickupRange, lootLayer);
            Debug.Log($" Poèet nalezených objektù: {loots.Length}");

            foreach (Collider2D col in loots)
            {
                Debug.Log($"   - Vidím objekt: {col.name}");
                LootPickup item = col.GetComponent<LootPickup>();
                if (item != null && item.canBePickedUp)
                {
                    Debug.Log("   -> Sbírám!");
                    item.Collect();
                    break;
                }
                else
                {
                    Debug.Log($"   -> Nemùžu sebrat (Item: {item != null}, CanPickup: {item?.canBePickedUp})");
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