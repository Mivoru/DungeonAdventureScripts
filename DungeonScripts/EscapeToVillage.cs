using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class EscapeToVillage : MonoBehaviour
{
    [Header("Settings")]
    public float holdTime = 5.0f; // Nastaveno na 5s podle zadání
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

        // Èteme input (zda držíš klávesu K)
        bool isHoldingEsc = escapeAction.ReadValue<float>() > 0.5f;

        // Kontrola stání (hráè se nesmí hýbat, aby mohl utéct)
        bool isStandingStill = (playerRb != null && playerRb.linearVelocity.magnitude < 0.1f);

        // Musí držet klávesu A ZÁROVEÒ stát na místì
        if (isHoldingEsc && isStandingStill)
        {
            if (!uiPanel.activeSelf) uiPanel.SetActive(true);

            // ZMÌNA: Používáme Time.deltaTime místo unscaledDeltaTime.
            // Pokud hru pauzneš (ESC), timer se zastaví (což je správnì).
            timer += Time.deltaTime;

            if (progressImage) progressImage.fillAmount = timer / holdTime;

            if (timer >= holdTime)
            {
                AudioManager.instance.PlaySFX("Portal");
                TeleportHome();
                timer = 0;
            }
        }
        else
        {
            // Reset, pokud pustíš klávesu nebo se pohneš
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
            // ZMÌNA: Tady musíme hledat pøesný název tvé nové akce
            escapeAction = playerInput.actions.FindAction("EscapeFromDungeon");

            playerRb = playerInput.GetComponent<Rigidbody2D>();
        }
    }

    void TeleportHome()
    {
        Debug.Log("Útìk do vesnice dokonèen!");

        // Reset èasu (pro jistotu, kdyby se nìco pokazilo s TimeScale)
        Time.timeScale = 1f;

        // Uložit hru pøed odchodem (volitelné, ale doporuèené)
        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveGame();
        }

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