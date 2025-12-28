using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class PauseMenu : MonoBehaviour
{
    public static bool isPaused = false;

    [Header("UI Panels")]
    public GameObject pauseMenuUI;     // Panel s tlaèítky Resume, Save, Exit...
    public GameObject settingsMenuUI;  // NOVÉ: Panel s nastavením

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Pokud jsme v nastavení, ESC nás vrátí do Pause Menu, ne do hry
            if (settingsMenuUI.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    void TogglePause()
    {
        if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false); // Pro jistotu vypneme i settings
        Time.timeScale = 1f;
        isPaused = false;

        if (TimeUI.instance != null) TimeUI.instance.ShowClock(true);
    }

    void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        if (TimeUI.instance != null) TimeUI.instance.ShowClock(false);
    }

    // --- Tlaèítka ---

    public void OnSaveButton()
    {
        if (SaveManager.instance != null)
        {
            SaveManager.instance.SaveGame();
        }
    }

    // UPRAVENO: Otevøe nastavení a skryje hlavní tlaèítka
    public void OnSettingsButton()
    {
        Debug.Log("Otevírám Settings...");
        pauseMenuUI.SetActive(false);    // Skryjeme Resume/Save/Exit
        settingsMenuUI.SetActive(true);  // Zobrazíme okno nastavení
    }

    // NOVÉ: Voláno tlaèítkem "Back" v nastavení
    public void CloseSettings()
    {
        settingsMenuUI.SetActive(false); // Skryjeme nastavení
        pauseMenuUI.SetActive(true);     // Vrátíme hlavní tlaèítka
    }

    public void OnExitButton()
    {
        Time.timeScale = 1f;
        if (SaveManager.instance != null) SaveManager.instance.SaveGame();
        Debug.Log("Konec hry.");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void OnExitToMenuButton()
    {
        Debug.Log("1. Kliknuto na Exit to Menu.");
        Time.timeScale = 1f;

        if (SaveManager.instance != null) SaveManager.instance.SaveGame();
        else Debug.LogError("CHYBA: SaveManager neexistuje!");

        SceneManager.LoadScene("MainMenu");
    }

    public void OnQuitGameButton()
    {
        if (SaveManager.instance != null) SaveManager.instance.SaveGame();
        Debug.Log("Ukonèuji hru a ukládám...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}