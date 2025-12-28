using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainSettingsButtons; // Panel s tlaèítky Audio, Graphics, Back
    public GameObject audioPanel;          // Panel se slidery (vnoøený)

    [Header("Audio Sliders")]
    public Slider musicSlider;
    public Slider sfxSlider;

    // Zavolá se vždy, když se okno nastavení aktivuje
    void OnEnable()
    {
        // Resetujeme zobrazení (vždy zaèít výbìrem kategorie)
        mainSettingsButtons.SetActive(true);
        audioPanel.SetActive(false);

        // Naèteme aktuální hodnoty do sliderù
        if (AudioManager.instance != null)
        {
            if (musicSlider != null) musicSlider.value = AudioManager.instance.GetMusicVolume();
            if (sfxSlider != null) sfxSlider.value = AudioManager.instance.GetSFXVolume();
        }
    }

    // --- Metody pro tlaèítka kategorií ---

    public void OpenAudioSettings()
    {
        mainSettingsButtons.SetActive(false);
        audioPanel.SetActive(true);
    }

    public void OpenGraphicsSettings()
    {
        Debug.Log("Grafika zatím není hotová.");
    }

    public void OpenControlsSettings()
    {
        Debug.Log("Ovládání zatím není hotové.");
    }

    // Tlaèítko "Back" uvnitø Audio panelu (vrátí nás na výbìr kategorií)
    public void BackToCategories()
    {
        audioPanel.SetActive(false);
        mainSettingsButtons.SetActive(true);
    }

    // --- Metody pro Slidery (propojit v Unity) ---

    public void SetMusicVolume(float volume)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetMusicVolume(volume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.SetSFXVolume(volume);
        }
    }
}