using UnityEngine;

public class DungeonExit : MonoBehaviour
{
    // Tuto metodu zavolá PlayerInteraction po stisku E
    public void Interact()
    {
        Debug.Log("Dungeon dokonèen! Vracím se do vesnice...");

        if (GameManager.instance != null)
        {
            GameManager.instance.CompleteLevel(); // Uloží postup a naète vesnici
        }
        else
        {
            Debug.LogError("Chybí GameManager!");
            // Záloha:
            UnityEngine.SceneManagement.SceneManager.LoadScene("VillageScene");
        }
    }
}