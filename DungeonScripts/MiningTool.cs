using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class MiningTool : MonoBehaviour
{
    [Header("Mining Settings")]
    public float range = 1.5f;
    public float miningAngle = 100f;
    public LayerMask resourceLayer;
    public int miningPower = 1;
    public float swingDuration = 0.3f; // Délka animace (doba, kdy nemùžeš znovu kliknout)

    private bool isSwinging = false;
    // private Quaternion defaultRot; // UŽ NEPOTØEBUJEME (viz níže)

    void Start()
    {
        // defaultRot = transform.localRotation;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        // Ochrana proti UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        if (context.performed && !isSwinging)
        {
            StartCoroutine(Swing());
        }
    }

    IEnumerator Swing()
    {
        isSwinging = true;

        Animator anim = GetComponentInParent<Animator>();
        if (anim != null)
        {
            // 1. DÙLEŽITÉ: Nastavíme rychlost na 1.
            // Pokud je toto na 0 (což na startu hry je), animace zamrzne na 1. snímku.
            // Skript meèe to dìlá taky, proto to po meèi fungovalo.
            anim.SetFloat("AttackSpeed", 1.0f);

            // 2. Použijeme Trigger (jako døív)
            anim.SetTrigger("Attack");
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFX("Mine");
        }

        MineCone();

        yield return new WaitForSeconds(swingDuration);

        isSwinging = false;
    }

    void MineCone()
    {
        Vector2 facingDir = Vector2.down;
        Animator anim = GetComponentInParent<Animator>();

        if (anim != null)
        {
            facingDir = new Vector2(anim.GetFloat("LastHorizontal"), anim.GetFloat("LastVertical"));
            if (facingDir.sqrMagnitude > 0.01f) facingDir.Normalize();
            else facingDir = Vector2.down;
        }

        Vector3 origin = transform.parent.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, resourceLayer);

        foreach (var hit in hits)
        {
            Vector2 dirToTarget = (hit.transform.position - origin).normalized;

            if (Vector2.Angle(facingDir, dirToTarget) < miningAngle / 2f)
            {
                ResourceNode node = hit.GetComponent<ResourceNode>();
                if (node != null)
                {
                    node.TakeHit(miningPower);

                    // --- ZAKOMENTOVÁNO (OPRAVA CHYBY) ---
                    // Zatím nemáme EffectManager, takže tohle schováme, aby hra fungovala.
                    // if (EffectManager.instance != null) 
                    //     EffectManager.instance.PlayMiningEffect(hit.transform.position);
                    // ------------------------------------

                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (transform.parent != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.parent.position, range);
        }
    }
}