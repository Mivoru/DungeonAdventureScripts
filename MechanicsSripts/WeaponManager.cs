using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons References")]
    public GameObject swordObject;
    public GameObject bowObject;
    public GameObject pickaxeObject; // NOVÉ: Krumpáè

    [Header("Weapon Scripts")]
    public SwordAttack swordScript;
    public BowController bowScript;
    public MiningTool pickaxeScript; // NOVÉ: Skript pro tìžbu

    // Aktuálnì vybraná zbraò (String ID)
    private string currentWeaponID = "Sword";

    void Start()
    {
        EquipWeaponByID("Sword"); // Defaultní zbraò
    }

    // Tuto metodu volá InventoryManager
    public void EquipWeaponByID(string id)
    {
        // Vypneme všechno
        if (swordObject) swordObject.SetActive(false);
        if (bowObject) bowObject.SetActive(false);
        if (pickaxeObject) pickaxeObject.SetActive(false);

        currentWeaponID = id;

        // Zapneme to správné
        switch (id)
        {
            case "Sword":
                if (swordObject) swordObject.SetActive(true);
                break;
            case "Bow":
                if (bowObject) bowObject.SetActive(true);
                break;
            case "Pickaxe":
                if (pickaxeObject) pickaxeObject.SetActive(true);
                break;
            default:
                Debug.LogWarning($"Neznámé ID zbranì: {id}");
                break;
        }
        Debug.Log($"Vybaveno: {id}");
    }

    public void OnMainAttack(InputAction.CallbackContext context)
    {
        // Pøesmìrujeme útok na správný skript
        switch (currentWeaponID)
        {
            case "Sword":
                if (swordScript) swordScript.OnAttack(context);
                break;
            case "Bow":
                if (bowScript) bowScript.OnAttack(context);
                break;
            case "Pickaxe":
                if (pickaxeScript) pickaxeScript.OnAttack(context);
                break;
        }
    }

    // (Staré metody OnSwitch1/2 mùžeš smazat, nebo nechat pro debug)
}