using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EnemyStats))]
public class SlimeAI : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpForce = 6f;      // Síla odrazu
    public float jumpInterval = 3f;   // Čas mezi skoky
    public float chargeTime = 0.6f;   // Jak dlouho se "krčí" před skokem
    public float aggroRange = 8f;     // Kdy začne útočit

    private Transform player;
    private Rigidbody2D rb;
    private Animator anim;

    private float nextJumpTime;
    private bool isCharging = false;
    private float slimeSize; // Pro správné otáčení

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        nextJumpTime = Time.time + 1f;
        slimeSize = transform.localScale.y; // Zapamatujeme si velikost z Inspectoru
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Pokud jsme na zemi (nehybeme se rychle) a vidíme hráče
        if (dist < aggroRange && !isCharging && Time.time >= nextJumpTime && rb.linearVelocity.magnitude < 0.1f)
        {
            // Otočení k hráči (jen když jsme na zemi)
            FacePlayer();

            StartCoroutine(PrepareAndJump());
        }
    }

    void FacePlayer()
    {
        if (player.position.x < transform.position.x)
            transform.localScale = new Vector3(-slimeSize, slimeSize, 1);
        else
            transform.localScale = new Vector3(slimeSize, slimeSize, 1);
    }

    IEnumerator PrepareAndJump()
    {
        isCharging = true;

        // 1. PŘÍPRAVA (Animace Charge/Idle)
        // Pokud máš animaci "Charge" nebo "PreJump", spusť ji tady
        // anim.SetTrigger("Charge");

        yield return new WaitForSeconds(chargeTime);

        // 2. SKOK
        if (player != null)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.AddForce(direction * jumpForce, ForceMode2D.Impulse);

            if (anim != null) anim.SetBool("IsJumping", true);
        }

        // Čekáme, až se "odlepí" od země (malá prodleva, aby velocity stihla naskočit)
        yield return new WaitForSeconds(0.1f);

        // 3. LET A DOPAD
        // Čekáme, dokud se nepřestane hýbat (dokud nedopadne a nedobrzdí)
        while (rb.linearVelocity.magnitude > 0.5f)
        {
            yield return null;
        }

        // Dopadl
        if (anim != null)
        {
            anim.SetBool("IsJumping", false);
            anim.SetTrigger("Attack"); // Spustí animaci "rozplácnutí"
        }

        // Tady se zavolá SlimeAttack přes fyzickou kolizi, nebo přes Animation Event, 
        // ale pro jistotu ho můžeme zavolat i odtud, pokud by kolize selhala.
        // Prozatím spoléháme na skript SlimeAttack (OnCollisionEnter2D).

        // 4. COOLDOWN
        isCharging = false;
        nextJumpTime = Time.time + jumpInterval;
    }

    // Voláno z EnemyStats při zásahu
    public void TriggerHurtAnim()
    {
        if (anim != null) anim.SetTrigger("Hit");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
    }
}