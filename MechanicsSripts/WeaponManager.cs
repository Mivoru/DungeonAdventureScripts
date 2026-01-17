using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Animators")]
    public Animator playerAnimator; // SEM pøetáhni Animator z Hráèe
    public AnimatorOverrideController unarmedController; // Sem dej Base (nebo Unarmed Override)
    public AnimatorOverrideController swordController;   // Sem dej Sword Override
    public AnimatorOverrideController pickaxeController; // Sem dej Pickaxe Override

    [Header("Weapons References")]
    public GameObject swordObject;
    public GameObject bowObject;
    public GameObject pickaxeObject;

    [Header("Weapon Scripts")]
    public SwordAttack swordScript;
    public BowController bowScript;
    public MiningTool pickaxeScript;

    // Aktuálnì vybraná zbraò (String ID)
    private string currentWeaponID = "Sword";

    void Start()
    {
        EquipWeaponByID("Sword"); // Defaultní zbraò
    }

    // Tuto metodu volá InventoryManager
    public void EquipWeaponByID(string id)
    {
        // 1. Vypneme vizuální objekty zbraní
        if (swordObject) swordObject.SetActive(false);
        if (bowObject) bowObject.SetActive(false);
        if (pickaxeObject) pickaxeObject.SetActive(false);

        if (playerAnimator != null) playerAnimator.ResetTrigger("Attack");
        currentWeaponID = id;

        // 2. Zapneme správný objekt A PØEPNEME ANIMATOR
        switch (id)
        {
            case "Sword":
                if (swordObject) swordObject.SetActive(true);

                // Pøepnutí na animace meèe
                if (playerAnimator != null && swordController != null)
                {
                    playerAnimator.runtimeAnimatorController = swordController;
                    playerAnimator.Play("Idle", 0, 0f);
                }
                break;

            case "Bow":
                if (bowObject) bowObject.SetActive(true);

                // Luk používá základní pohyb (Unarmed), nebo si pro nìj udìlej extra override
                if (playerAnimator != null && unarmedController != null)
                {
                    playerAnimator.runtimeAnimatorController = unarmedController;
                    playerAnimator.Play("Idle", 0, 0f);
                }
                break;

            case "Pickaxe":
                if (pickaxeObject) pickaxeObject.SetActive(true);

                // Pøepnutí na animace krumpáèe
                if (playerAnimator != null && pickaxeController != null)
                {
                    playerAnimator.runtimeAnimatorController = pickaxeController;
                    playerAnimator.Play("Idle", 0, 0f);
                }
                break;

            default: // Unarmed nebo neznámé ID
                // Vrátíme základní animace
                if (playerAnimator != null && unarmedController != null)
                {
                    playerAnimator.runtimeAnimatorController = unarmedController;
                }
                Debug.LogWarning($"Neznámé ID zbranì nebo Unarmed: {id}");
                break;
        }
        if (playerAnimator != null)
        {
            playerAnimator.Update(0f); // Donutí Animator pøepoèítat stav HNED TEÏ
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
}