using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EscapeToVillage : MonoBehaviour
{
    [Header("Settings")]
    public float holdTime = 2.0f; // Zkráceno na 2s pro lepší testování
    public Image progressImage;
    public GameObject uiPanel;

    private float timer = 0f;
    private Rigidbody2D playerRb;
    private InputAction escapeAction;
    private PlayerInput playerInput;

    void Update()
    {
        // Ve vesnici skript nic nedìlá
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VillageScene")
        {
            if (uiPanel.activeSelf) uiPanel.SetActive(false);
            return;
        }

        // Pokud nemáme reference, zkusíme je najít (napø. po naètení scény)
        if (playerInput == null || escapeAction == null)
        {
            FindPlayer();
            return;
        }

        // Èteme input (funguje i když je Time.timeScale = 0)
        bool isHoldingEsc = escapeAction.ReadValue<float>() > 0.5f;

        // Kontrola stání
        bool isStandingStill = (playerRb != null && playerRb.linearVelocity.magnitude < 0.1f);

        if (isHoldingEsc && isStandingStill)
        {
            if (!uiPanel.activeSelf) uiPanel.SetActive(true);

            // Používáme unscaledDeltaTime, aby to fungovalo i pøi pauze
            timer += Time.unscaledDeltaTime;

            if (progressImage) progressImage.fillAmount = timer / holdTime;

            if (timer >= holdTime)
            {
                TeleportHome();
                timer = 0;
            }
        }
        else
        {
            // Reset
            if (timer > 0)
            {
                timer = 0;
                if (progressImage) progressImage.fillAmount = 0;
                if (uiPanel.activeSelf) uiPanel.SetActive(false);
            }
        }
    }

    void FindPlayer()
    {
        playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            escapeAction = playerInput.actions.FindAction("Escape");
            playerRb = playerInput.GetComponent<Rigidbody2D>();
        }
    }

    void TeleportHome()
    {
        Debug.Log("Útìk do vesnice!");
        // Reset èasu pro jistotu
        Time.timeScale = 1f;

        if (GameManager.instance != null)
        {
            GameManager.instance.ReturnToVillage();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
        }
    }
}