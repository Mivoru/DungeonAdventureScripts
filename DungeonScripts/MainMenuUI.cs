using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;


public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text difficultyText;
    public GameObject continueButton;

    [Header("Panels")]
    public GameObject mainButtonsPanel; // SEM pøetáhni objekt, ve kterém máš tlaèítka (New Game, Quit, atd.)
    public GameObject settingsPanel;    // SEM pøetáhni ten Prefab SettingsWindow (který je ve scénì vypnutý)

    void Start()
    {
        UpdateDifficultyText();

        // Zajistíme, že na startu je vidìt menu a ne settings
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Kontrola Savu
        if (SaveManager.instance != null && SaveManager.instance.HasSaveFile())
        {
            if (continueButton != null) continueButton.SetActive(true);
        }
        else
        {
            if (continueButton != null) continueButton.SetActive(false);
        }
    }

    // --- TOTO PØIDEJ PRO SETTINGS ---
    public void OnSettingsClicked()
    {
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(false); // Skryjeme menu
        if (settingsPanel != null) settingsPanel.SetActive(true);       // Ukážeme settings
    }

    public void OnCloseSettingsClicked()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);      // Skryjeme settings
        if (mainButtonsPanel != null) mainButtonsPanel.SetActive(true); // Vrátíme menu
    }
    // --------------------------------

    public void OnContinueClicked()
    {
        Debug.Log("Pokraèuji ve høe...");
        SaveManager.shouldLoadAfterSceneChange = true;
        SceneManager.LoadScene("VillageScene");
    }

    public void OnNewGameClicked()
    {
        Debug.Log("Startuji NOVOU hru...");
        SaveManager.shouldLoadAfterSceneChange = false;
        SceneManager.LoadScene("VillageScene");
    }

    public void OnDifficultyToggle()
    {
        if (GameManager.instance == null) return;
        int current = (int)GameManager.instance.currentDifficulty;
        int next = (current == 0) ? 1 : 0;
        GameManager.instance.SetDifficulty(next);
        UpdateDifficultyText();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Vypínám hru...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void UpdateDifficultyText()
    {
        if (GameManager.instance != null && difficultyText != null)
        {
            if (GameManager.instance.currentDifficulty == GameManager.Difficulty.Normal)
            {
                difficultyText.text = "Difficulty: Normal";
                difficultyText.color = Color.black;
            }
            else
            {
                difficultyText.text = "Difficulty: HARD";
                difficultyText.color = Color.red;
            }
        }
    }
}