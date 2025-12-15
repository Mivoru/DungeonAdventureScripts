using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager instance;

    [Header("Settings")]
    public float dayDurationInMinutes = 24f; // Celý den trvá 24 reálných minut

    [Header("Current Time")]
    public float currentTime = 0.5f; // 0.0 až 1.0 (0.5 = poledne)
    public int daysPassed = 1;

    // Pomocné promìnné
    public float Hours => currentTime * 24f;
    public float Minutes => (currentTime * 24f % 1) * 60f;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Update()
    {
        // Výpoèet rychlosti: 1 / (minuty * 60 sekund)
        float speed = 1f / (dayDurationInMinutes * 60f);

        currentTime += Time.deltaTime * speed;

        if (currentTime >= 1f)
        {
            currentTime = 0f;
            daysPassed++;
        }
    }

    public string GetTimeString()
    {
        // Formátování èasu na HH:MM
        return TimeSpan.FromHours(Hours).ToString(@"hh\:mm");
    }
}