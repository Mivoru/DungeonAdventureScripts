using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI; // Potøeba pro ovládání Buttonù (skrývání)

public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text difficultyText;
    public GameObject continueButton; // SEM pøetáhni tlaèítko Continue

    void Start()
    {
        UpdateDifficultyText();

        // KONTROLA SAVU:
        // Pokud existuje SaveManager a existuje soubor se savem -> Ukážeme Continue
        if (SaveManager.instance != null && SaveManager.instance.HasSaveFile())
        {
            if (continueButton != null) continueButton.SetActive(true);
        }
        else
        {
            // Jinak tlaèítko Continue schováme
            if (continueButton != null) continueButton.SetActive(false);
        }
    }

    public void OnContinueClicked()
    {
        Debug.Log("Pokraèuji ve høe... (Nastavuji load = true)");

        // ZMÌNA: Mažeme '.instance'. Voláme to pøímo.
        SaveManager.shouldLoadAfterSceneChange = true;

        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
    }

    public void OnNewGameClicked()
    {
        Debug.Log("Startuji NOVOU hru...");

        // ZMÌNA: Mažeme '.instance'.
        SaveManager.shouldLoadAfterSceneChange = false;

        UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
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

        // TENTO KÓD ZAØÍDÍ, ŽE TO FUNGUJE I V UNITY EDITORU
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