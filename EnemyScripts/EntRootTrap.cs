using System.Collections;
using UnityEngine;

public class EntRootTrap : MonoBehaviour
{
    [Header("Settings")]
    public float warningTime = 0.5f; // Jak dlouho má hráè na útìk
    public float stunDuration = 2.0f; // Jak dlouho bude stát
    public int damage = 10; // Malý damage, hlavnì stun

    [Header("Visuals")]
    public Sprite warningSprite; // Kruh na zemi (èervený?)
    public Sprite activeSprite;  // Vyrostlé koøeny

    private SpriteRenderer sr;
    private bool isActive = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (warningSprite) sr.sprite = warningSprite;

        // Zaèneme odpoèet varování
        StartCoroutine(TrapRoutine());
    }

    IEnumerator TrapRoutine()
    {
        // 1. FÁZE: VAROVÁNÍ (Barva tøeba do èervena)
        sr.color = new Color(1, 0, 0, 0.5f); // Poloprùhledná èervená
        yield return new WaitForSeconds(warningTime);

        // 2. FÁZE: AKTIVACE (CHÒAP!)
        isActive = true;
        if (activeSprite) sr.sprite = activeSprite;
        sr.color = Color.white; // Normální barva

        // Zkontrolujeme, kdo v tom stojí TEÏ
        CheckCapture();

        // Necháme chvíli viditelné koøeny a pak zmizíme
        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    void CheckCapture()
    {
        // Kruhový test kolize
        Collider2D hit = Physics2D.OverlapCircle(transform.position, 0.5f, LayerMask.GetMask("Player"));

        if (hit != null)
        {
            PlayerStats stats = hit.GetComponent<PlayerStats>();
            PlayerMovement move = hit.GetComponent<PlayerMovement>();

            if (stats != null) stats.TakeDamage(damage);

            // APLIKACE STUNU (Zpomalení na 0%)
            if (move != null)
            {
                Debug.Log("HRÁÈ CHYCEN DO KOØENÙ!");
                move.ApplySlow(stunDuration, 0f); // 0f = Úplné zastavení
            }
        }
    }
}