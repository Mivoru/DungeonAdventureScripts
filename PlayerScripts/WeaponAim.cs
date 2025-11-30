using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponAim : MonoBehaviour
{
    public Camera mainCamera;

    void Update()
    {
        // Získáme pozici myši
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // Smìr od zbranì k myši
        Vector3 aimDirection = (mousePos - transform.position).normalized;

        // Vypoèítáme úhel
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        // Otoèíme zbraò (zbraò se toèí nezávisle na tìle hráèe)
        transform.rotation = Quaternion.Euler(0, 0, angle - 90f); // -90 korekce, pokud sprite smìøuje nahoru
    }
}