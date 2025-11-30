using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class HealthBar : MonoBehaviour
{
    [Tooltip("Drag the foreground 'Fill' image here")]
    public Image fillImage;

    // Call this method to update the visual bar
    // currentHealth: current HP
    // maxHealth: maximum possible HP
    public void UpdateBar(float currentHealth, float maxHealth)
    {
        if (fillImage != null)
        {
            // Calculate fill percentage (0.0 to 1.0)
            float fillAmount = currentHealth / maxHealth;
            fillImage.fillAmount = fillAmount;
        }
    }
}