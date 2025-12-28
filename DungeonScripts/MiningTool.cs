using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiningTool : MonoBehaviour
{
    [Header("Mining Settings")]
    public float range = 1.5f;      // Dosah (jako u meèe)
    public float miningAngle = 100f; // Šíøka zábìru (výseè)
    public LayerMask resourceLayer;  // Vrstva "Resources"
    public int miningPower = 1;      // Síla kopnutí
    public float swingDuration = 0.3f;

    private bool isSwinging = false;
    private Quaternion defaultRot;

    void Start()
    {
        defaultRot = transform.localRotation;
    }

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

        // 1. Logika Tìžby (Výseè)
        MineCone();
        AudioManager.instance.PlaySFX("Mine");
        // 2. Animace nástroje (vizuální otoèení)
        float timer = 0f;
        Quaternion targetRot = Quaternion.Euler(0, 0, -50f);

        while (timer < swingDuration)
        {
            transform.localRotation = Quaternion.Lerp(defaultRot, targetRot, timer / swingDuration);
            timer += Time.deltaTime;
            yield return null;
        }

        // Návrat zpìt
        transform.localRotation = defaultRot;
        isSwinging = false;
    }

    void MineCone()
    {
        // A) Zjištìní smìru (Podle Animátoru hráèe)
        Vector2 facingDir = Vector2.down; // Default
        Animator anim = GetComponentInParent<Animator>();

        if (anim != null)
        {
            facingDir = new Vector2(anim.GetFloat("LastHorizontal"), anim.GetFloat("LastVertical"));
            if (facingDir.sqrMagnitude > 0.01f) facingDir.Normalize();
            else facingDir = Vector2.down;
        }

        // B) Støed tìžby (Od støedu hráèe)
        Vector3 origin = transform.parent.position;

        // C) Hledáme rudy v kruhu
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, resourceLayer);

        foreach (var hit in hits)
        {
            Vector2 dirToTarget = (hit.transform.position - origin).normalized;

            // D) Kontrola Úhlu (Je ruda pøed námi?)
            if (Vector2.Angle(facingDir, dirToTarget) < miningAngle / 2f)
            {
                ResourceNode node = hit.GetComponent<ResourceNode>();
                if (node != null)
                {
                    node.TakeHit(miningPower);
                    return; // Vytìžíme jen jednu rudu na jeden švih (nebo to smaž, pokud chceš plošnou tìžbu)
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Vizualizace dosahu (pokud je skript pøipojen)
        if (transform.parent != null)
        {
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.parent.position, range);
        }
    }
}