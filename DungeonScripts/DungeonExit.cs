using UnityEngine;
using UnityEngine.SceneManagement;

public class DungeonExit : MonoBehaviour
{
    [Header("Settings")]
    public int floorToUnlock = 2; // Pokud jsi v patøe 1, nastav sem 2
    public string sceneToLoad = "VillageScene";

    // Tuto metodu zavolá PlayerInteraction po stisku E
    public void Interact()
    {
        Debug.Log("Dungeon dokonèen! Ukládám postup...");

        // 1. KROK: Odemkneme další patro v SaveManageru
        if (SaveManager.instance != null)
        {
            SaveManager.instance.UnlockFloor(floorToUnlock);
        }

        // 2. KROK: Návrat do vesnice
        // Mùžeme použít GameManager, pokud tam máš nìjaké stmívaèky/pøechody
        if (GameManager.instance != null)
        {
            // Pokud CompleteLevel() jen naèítá scénu, je to OK.
            // Pokud v CompleteLevel() máš taky nìjaké ukládání, nevadí to (uloží se to 2x, to je sichr).
            GameManager.instance.CompleteLevel();
        }
        else
        {
            // Záloha, kdyby GameManager nebyl
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}