using UnityEngine;
using TMPro;

public class TimeUI : MonoBehaviour
{
    public TMP_Text timeText;
    public TMP_Text dayText;

    void Update()
    {
        if (TimeManager.instance != null)
        {
            if (timeText) timeText.text = TimeManager.instance.GetTimeString();
            if (dayText) dayText.text = "Day " + TimeManager.instance.daysPassed;
        }
    }
}