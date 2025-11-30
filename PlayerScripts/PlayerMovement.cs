using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float sprintSpeedMultiplier = 1.8f;

    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.0f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Camera mainCamera;
    private PlayerStats stats;
    private Animator anim; // PŘIDÁNO: Proměnná pro animátor

    private Vector2 movementInput;
    private Vector2 mousePosition;
    private float currentSpeed;
    private float slowMultiplier = 1f;

    private bool isDashing = false;
    private bool canDash = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        mainCamera = Camera.main;
        stats = GetComponent<PlayerStats>();

        // PŘIDÁNO: Získání animátoru
        anim = GetComponent<Animator>();

        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (playerInput != null)
        {
            playerInput.actions.FindActionMap("Player").Enable();
        }
    }

    // Zajistíme, že mapa Player bude aktivní (pojistka)
    void Start()
    {
        if (playerInput != null) playerInput.SwitchCurrentActionMap("Player");
        if (stats != null) currentSpeed = stats.movementSpeed; // Inicializace rychlosti
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        mousePosition = context.ReadValue<Vector2>();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (stats == null) return;

        if (context.performed)
            currentSpeed = stats.movementSpeed * sprintSpeedMultiplier;
        else if (context.canceled)
            currentSpeed = stats.movementSpeed;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed && canDash && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    void Update()
    {
        if (isDashing) return;

        // 1. Získáme pozici myši ve světě
        Vector3 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);

        // Vektor směru od hráče k myši (Normalized = délka 1)
        Vector2 lookDir = (mouseWorldPos - transform.position).normalized;

        // ---------------------------------------------------------
        // TOTO JE TA ČÁST, KTEROU JSME ZAKOMENTOVALI (FYZICKÁ ROTACE)
        // Protože používáme 4-směrové animace, nesmíme točit objektem.
        /*
        Vector3 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        */
        // ---------------------------------------------------------

        // --- OVLÁDÁNÍ ANIMÁTORU ---
        if (anim != null)
        {
            // A. Rychlost (Speed)
            // Určuje, jestli hrajeme animaci "Idle" nebo "Movement".
            // Používáme movementInput (WASD), abychom věděli, jestli se hýbeme.
            float moveAmount = movementInput.magnitude;

            // Pokud se hýbeme, pošleme tam aktuální rychlost (aby se případně přeplo na běh)
            // Pokud stojíme, pošleme 0.
            if (moveAmount > 0)
            {
                anim.SetFloat("Speed", currentSpeed > 0 ? currentSpeed : 1f);
            }
            else
            {
                anim.SetFloat("Speed", 0f);
            }

            // B. Směr (Horizontal, Vertical)
            // DŮLEŽITÉ: Posíláme tam směr K MYŠI (lookDir), ne směr chůze!
            // Díky tomu budeš moci couvat a střílet na nepřítele (Strafing).

            anim.SetFloat("Horizontal", lookDir.x);
            anim.SetFloat("Vertical", lookDir.y);

            // C. Paměť (LastHorizontal, LastVertical)
            // Ukládáme, kam jsme koukali naposledy, aby Idle animace zůstala otočená správně.
            anim.SetFloat("LastHorizontal", lookDir.x);
            anim.SetFloat("LastVertical", lookDir.y);
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;

        // Pohyb zůstává podle klávesnice (WASD)
        // Bez ohledu na to, kam koukáme
        float targetSprintSpeed = (stats != null) ? stats.movementSpeed * sprintSpeedMultiplier : 5f;

        if (currentSpeed < targetSprintSpeed && currentSpeed != (stats ? stats.movementSpeed : 5f))
            currentSpeed = (stats ? stats.movementSpeed : 5f);

        if (currentSpeed == 0) currentSpeed = (stats ? stats.movementSpeed : 5f);

        Vector2 targetVelocity = movementInput * currentSpeed * slowMultiplier;
        rb.linearVelocity = targetVelocity;
    }

    public void ApplySlow(float duration, float slowAmount)
    {
        StartCoroutine(SlowRoutine(duration, slowAmount));
    }

    IEnumerator SlowRoutine(float duration, float slowAmount)
    {
        slowMultiplier = slowAmount;
        yield return new WaitForSeconds(duration);
        slowMultiplier = 1f;
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;

        Vector2 dashDirection = movementInput.normalized;
        if (dashDirection == Vector2.zero) dashDirection = transform.up;
        // Pozor: Pokud jsme vypnuli rotaci transformu, transform.up bude vždy nahoru.
        // Lepší je použít poslední uložený směr z Animátoru, nebo default (0,1).
        if (dashDirection == Vector2.zero) dashDirection = new Vector2(0, 1);

        Vector2 startPosition = rb.position;
        Vector2 endPosition = startPosition + dashDirection * dashDistance;

        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            float t = (Time.time - startTime) / dashDuration;
            rb.MovePosition(Vector2.Lerp(startPosition, endPosition, t));
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}