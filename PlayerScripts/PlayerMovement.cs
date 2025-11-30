using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    // Rychlost už nebereme veřejně, ale ze statistik
    public float sprintSpeedMultiplier = 1.8f;

    [Header("Dash Settings")]
    public float dashDistance = 4f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 1.0f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private Camera mainCamera;
    private PlayerStats stats; // Odkaz na statistiky

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

        // Získáme statistiky z komponenty PlayerStats
        stats = GetComponent<PlayerStats>();

        rb.gravityScale = 0;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Aktivace Input Mapy
        if (playerInput != null)
        {
            playerInput.actions.FindActionMap("Player").Enable();
        }
    }

    // === INPUT EVENTY ===
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
        // Pojistka, kdyby stats chyběly
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
        if (!isDashing)
        {
            // Rotace za myší
            Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            Vector3 direction = mouseWorldPosition - transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }

    void FixedUpdate()
    {
        if (isDashing) return;
        if (stats == null) return;

        // Logika pro Sprint (stejná jako předtím)
        float targetSprintSpeed = stats.movementSpeed * sprintSpeedMultiplier;
        if (currentSpeed < targetSprintSpeed && currentSpeed != stats.movementSpeed)
        {
            currentSpeed = stats.movementSpeed;
        }
        if (currentSpeed == 0) currentSpeed = stats.movementSpeed;

        // ZMĚNA: Aplikujeme slowMultiplier do finálního výpočtu
        Vector2 targetVelocity = movementInput * currentSpeed * slowMultiplier;

        rb.linearVelocity = targetVelocity;
    }

    public void ApplySlow(float duration, float slowAmount)
    {
        // Spustíme Coroutinu, která hráče zpomalí a pak vrátí rychlost zpět
        StartCoroutine(SlowRoutine(duration, slowAmount));
    }

    IEnumerator SlowRoutine(float duration, float slowAmount)
    {
        // slowAmount 0.5 znamená 50% rychlost
        slowMultiplier = slowAmount;
        // Debug.Log("Hráč je zpomalen!");

        yield return new WaitForSeconds(duration);

        slowMultiplier = 1f; // Návrat k normálu
        // Debug.Log("Hráč už není zpomalen.");
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        canDash = false;

        Vector2 dashDirection = movementInput.normalized;
        if (dashDirection == Vector2.zero) dashDirection = transform.up; // Fallback na směr pohledu

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