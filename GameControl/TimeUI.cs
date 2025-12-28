using UnityEngine;
using TMPro;

public class TimeUI : MonoBehaviour
{
    public static TimeUI instance; // Singleton pro pøístup odkudkoliv

    [Header("UI Elements")]
    public GameObject clockPanel; // SEM v Inspectoru pøetáhni celý Panel/GameObject s hodinami
    public TMP_Text timeText;
    public TMP_Text dayText;

    void Awake()
    {
        // Nastavení Singletonu
        if (instance == null) instance = this;
    }

    void Update()
    {
        if (TimeManager.instance != null)
        {
            if (timeText) timeText.text = TimeManager.instance.GetTimeString();
            if (dayText) dayText.text = "Day " + TimeManager.instance.daysPassed;
        }
    }

    // Tuto metodu budeme volat z ostatních skriptù
    public void ShowClock(bool show)
    {
        if (clockPanel != null)
        {
            clockPanel.SetActive(show);
        }
    }
}