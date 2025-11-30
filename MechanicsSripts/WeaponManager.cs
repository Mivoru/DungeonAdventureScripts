using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapons References")]
    public GameObject swordObject; // Celý objekt SwordHolder
    public GameObject bowObject;   // Celý objekt BowHolder (s FirePointem)

    [Header("Weapon Scripts")]
    public SwordAttack swordScript; // Skript na meèi
    public BowController bowScript; // Skript na luku

    // 1 = Meè, 2 = Luk
    private int currentWeapon = 1;

    void Start()
    {
        // Na zaèátku zapneme meè a vypneme luk
        EquipSword();
    }

    // === PØEPÍNÁNÍ ZBRANÍ (Voláno z Input Systemu) ===

    public void OnSwitch1(InputAction.CallbackContext context)
    {
        if (context.performed) EquipSword();
    }

    public void OnSwitch2(InputAction.CallbackContext context)
    {
        if (context.performed) EquipBow();
    }

    void EquipSword()
    {
        currentWeapon = 1;
        swordObject.SetActive(true);  // Zviditelnit meè
        bowObject.SetActive(false);   // Skrýt luk
        Debug.Log("Vybaven: MEÈ");
    }

    void EquipBow()
    {
        currentWeapon = 2;
        swordObject.SetActive(false); // Skrýt meè
        bowObject.SetActive(true);    // Zviditelnit luk
        Debug.Log("Vybaven: LUK");
    }

    // === HLAVNÍ ÚTOK (Voláno tlaèítkem myši) ===

    // Tuto funkci propojíme v Player Inputu místo pøímých zbraní!
    public void OnMainAttack(InputAction.CallbackContext context)
    {
        // Manažer zjistí, co držíš, a pošle signál správnému skriptu
        if (currentWeapon == 1)
        {
            swordScript.OnAttack(context);
        }
        else if (currentWeapon == 2)
        {
            bowScript.OnAttack(context);
        }
    }
}