using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuUI : MonoBehaviour
{
    public void OnPlayClicked()
    {
        // Naèteme vesnici
        SceneManager.LoadScene("VillageScene");
    }

    public void OnDifficultyChanged(int index)
    {
        // 0 = Normal, 1 = Hard (z Dropdownu)
        if (GameManager.instance != null)
        {
            GameManager.instance.SetDifficulty(index);
        }
    }

    public void OnQuitClicked()
    {
        Application.Quit();
    }
}