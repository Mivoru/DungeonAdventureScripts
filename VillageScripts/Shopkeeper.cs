using UnityEngine;
using System.Collections.Generic;

public class Shopkeeper : MonoBehaviour
{
    [Header("Shop Settings")]
    public string shopName = "General Store";
    public List<ItemData> itemsForSale; // Co prodává

    [Header("UI Reference")]
    // Odkaz na UI Obchodu (najde si ho sám nebo ho pøetáhneš)
    public ShopUI shopUI;

    void Start()
    {
        // Automaticky najdeme ShopUI ve scénì, pokud není pøiøazené
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }
    }

    // Tuto metodu zavolá PlayerInteraction (klávesa E)
    public void Interact()
    {
        // POJISTKA: Pokud shopUI chybí (Start nestihl dobìhnout nebo selhal), najdeme ho teï
        if (shopUI == null)
        {
            shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        }
        if (shopUI != null)
        {
            shopUI.ToggleShop(this);
        }
        else
        {
            Debug.LogError("CHYBA: Shopkeeper nemùže najít ShopUI v celé scénì!");
        }
    }
}