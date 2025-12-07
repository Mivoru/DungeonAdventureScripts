using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;


public class MainMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text difficultyText; // Text uvnitø tlaèítka (aby se mìnil nápis)

    void Start()
    {
        // Pøi startu menu nastavíme správný nápis podle toho, co je v GameManageru
        UpdateDifficultyText();
    }

    public void OnPlayClicked()
    {
        Debug.Log("Startuji hru...");
        SceneManager.LoadScene("VillageScene");
    }

    public void OnDifficultyToggle()
    {
        if (GameManager.instance == null) return;

        // Zjistíme aktuální (0 = Normal, 1 = Hard)
        int current = (int)GameManager.instance.currentDifficulty;

        // Pøepneme (pokud je 0, bude 1. Pokud je 1, bude 0)
        int next = (current == 0) ? 1 : 0;

        // Uložíme do GameManageru
        GameManager.instance.SetDifficulty(next);

        // Aktualizujeme nápis
        UpdateDifficultyText();
    }

    public void OnQuitClicked()
    {
        Debug.Log("Vypínám hru...");
        Application.Quit();
    }

    void UpdateDifficultyText()
    {
        if (GameManager.instance != null && difficultyText != null)
        {
            // Zmìníme text podle aktuální obtížnosti
            if (GameManager.instance.currentDifficulty == GameManager.Difficulty.Normal)
            {
                difficultyText.text = "Difficulty: Normal";
                difficultyText.color = Color.black; // (Volitelné)
            }
            else
            {
                difficultyText.text = "Difficulty: HARD";
                difficultyText.color = Color.red; // (Volitelné: Èervená pro Hard)
            }
        }
    }
}