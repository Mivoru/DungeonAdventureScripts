using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // Nutné pro Input System

public class LevelUpUI : MonoBehaviour
{
    [Header("References")]
    public GameObject uiPanel;
    public TMP_Text pointsText;

    [Header("Stat Texts")]
    public TMP_Text defenseText;
    public TMP_Text critChanceText;
    public TMP_Text critDmgText;
    public TMP_Text atkSpeedText;
    public TMP_Text dashText;
    public TMP_Text luckText;
    public TMP_Text regenText;

    public Button[] upgradeButtons;

    // Promìnná pro uložení akce
    private InputAction menuAction;

    void Start()
    {
        // 1. Registrace u PlayerStats
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.levelUpUI = this;
        }

        UpdateUI();
        if (uiPanel != null) uiPanel.SetActive(false);

        // 2. NAPOJENÍ NA INPUT SYSTEM
        // Najdeme PlayerInput ve scénì (je na hráèi)
        var playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            // Najdeme akci podle názvu, který jsi zadal v editoru ("LevelMenu")
            menuAction = playerInput.actions.FindAction("LevelMenu");

            if (menuAction != null)
            {
                // Øekneme, co se má stát pøi stisku
                menuAction.performed += OnMenuToggle;
                menuAction.Enable(); // Pro jistotu zapneme
            }
            else
            {
                Debug.LogError("LevelUpUI: Akce 'LevelMenu' nebyla nalezena! Pøidal jsi ji do Input Actions?");
            }
        }
    }

    // Tuto metodu musíme odpojit, když se objekt znièí (aby nevznikaly chyby)
    void OnDestroy()
    {
        if (menuAction != null)
        {
            menuAction.performed -= OnMenuToggle;
        }
    }

    // Tato metoda se zavolá automaticky pøi stisku L
    private void OnMenuToggle(InputAction.CallbackContext context)
    {
        ToggleMenu();
    }

    public void ToggleMenu()
    {
        if (uiPanel != null)
        {
            bool isActive = !uiPanel.activeSelf;
            uiPanel.SetActive(isActive);

            // NOVÉ: Vypínání/Zapínání hodin
            // Pokud je menu aktivní (isActive == true), chceme hodiny schovat (ShowClock false)
            if (TimeUI.instance != null)
            {
                TimeUI.instance.ShowClock(!isActive);
            }

            if (isActive)
            {
                UpdateUI();
                // Volitelné: Zastavit èas
                // Time.timeScale = 0f; 
            }
            else
            {
                // Volitelné: Pustit èas
                // Time.timeScale = 1f;
            }
        }
    }

    public void UpdateUI()
    {
        if (PlayerStats.instance == null) return;
        PlayerStats p = PlayerStats.instance;

        if (pointsText) pointsText.text = $"Points: {p.statPoints}";

        if (defenseText) defenseText.text = p.defense.ToString();
        if (critChanceText) critChanceText.text = $"{p.critChance}%";
        if (critDmgText) critDmgText.text = $"{p.critDamage:F1}x";
        if (atkSpeedText) atkSpeedText.text = $"{p.attackSpeed:F2}x";
        if (dashText) dashText.text = $"-{p.dashCooldownRed:F1}s";
        if (luckText) luckText.text = $"{p.luck:F1}x";
        if (regenText) regenText.text = $"{p.regeneration}/s";

        bool canUpgrade = p.statPoints > 0;
        foreach (var btn in upgradeButtons)
        {
            if (btn) btn.interactable = canUpgrade;
        }
    }

    public void OnUpgradeClick(string statName)
    {
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.UpgradeStat(statName);
        }
    }
}