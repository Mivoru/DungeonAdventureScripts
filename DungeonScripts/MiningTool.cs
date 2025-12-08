using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiningTool : MonoBehaviour
{
    [Header("Settings")]
    public Transform attackPoint;
    public float range = 1.0f;
    public LayerMask resourceLayer; // Vrstva "Resources"
    public int miningPower = 1;     // Kolik ubere HP rudì
    public float swingDuration = 0.3f;

    private bool isSwinging = false;
    private Quaternion defaultRot;

    void Start()
    {
        defaultRot = transform.localRotation;
    }

    // Voláno z WeaponManageru
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && !isSwinging)
        {
            StartCoroutine(Swing());
        }
    }

    IEnumerator Swing()
    {
        isSwinging = true;

        // 1. Animace (jednoduché otoèení, jako meè)
        float timer = 0f;
        Quaternion targetRot = Quaternion.Euler(0, 0, -45f); // Švih dolù

        while (timer < swingDuration)
        {
            transform.localRotation = Quaternion.Lerp(defaultRot, targetRot, timer / swingDuration);
            timer += Time.deltaTime;

            // Zásah v polovinì animace
            if (timer >= swingDuration * 0.5f && timer < (swingDuration * 0.5f) + Time.deltaTime)
            {
                CheckHit();
            }
            yield return null;
        }

        transform.localRotation = defaultRot; // Zpìt
        isSwinging = false;
    }

    void CheckHit()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, range, resourceLayer);
        foreach (var hit in hits)
        {
            ResourceNode node = hit.GetComponent<ResourceNode>();
            if (node != null)
            {
                node.TakeHit(miningPower);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(attackPoint.position, range);
        }
    }
}