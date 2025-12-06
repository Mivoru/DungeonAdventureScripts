using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public enum Difficulty { Normal, Hard }

    [Header("Game Settings")]
    public Difficulty currentDifficulty = Difficulty.Normal;

    [Header("Progression")]
    public int currentFloor = 1;
    public int maxUnlockedFloor = 1;
    public DungeonLevelData currentLevelData;

    [Header("Scene Names")]
    public string dungeonSceneName = "DungeonScene";
    public string villageSceneName = "VillageScene";
    public string menuSceneName = "MainMenu";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Voláno z Main Menu
    public void SetDifficulty(int difficultyIndex)
    {
        // 0 = Normal, 1 = Hard
        currentDifficulty = (Difficulty)difficultyIndex;
        Debug.Log($"Difficulty set to: {currentDifficulty}");
    }

    // Voláno pøi vstupu do Dungeonu (z portálu ve vesnici)
    public void EnterDungeon(DungeonLevelData levelData)
    {
        currentLevelData = levelData;

        // Aktualizujeme aktuální patro podle vybraných dat
        if (levelData != null)
        {
            currentFloor = levelData.floorIndex;
        }

        // 1. ULOŽÍME SNAPSHOT (Stav pøed vstupem)
        if (PlayerStats.instance != null) PlayerStats.instance.SaveSnapshot();
        if (InventoryManager.instance != null) InventoryManager.instance.SaveSnapshot();

        // Používáme LoadSceneMode.Single pro jistotu
        SceneManager.LoadScene(dungeonSceneName, LoadSceneMode.Single);
    }

    // Voláno pøi úspìšném dokonèení (Portál po bossovi)
    // POZOR: Pokud uteèeš pøes ESC, mìl bys volat spíše ReturnToVillage(), 
    // abys neodemkl další level za útìk.
    public void CompleteLevel()
    {
        Debug.Log($"Level {currentFloor} Completed! Saving Progress.");

        // --- OPRAVA: ODEMÈENÍ DALŠÍHO PATRA ---
        // Pokud jsme právì dokonèili naše nejvyšší odemèené patro, odemkneme další.
        if (currentFloor == maxUnlockedFloor)
        {
            maxUnlockedFloor++;
            Debug.Log($"New Floor Unlocked! Max Floor is now: {maxUnlockedFloor}");
        }
        // ---------------------------------------

        ReturnToVillage();
    }

    // Voláno pøi SMRTI (z PlayerStats)
    public void HandleDeathPenalty()
    {
        Debug.Log($"Handling Death Penalty for {currentDifficulty} Mode...");

        // 1. RESTORE STATS (V obou módech vracíme level na stav pøed vstupem)
        if (PlayerStats.instance != null)
        {
            PlayerStats.instance.LoadSnapshot();
        }

        // 2. INVENTORY PENALTY
        if (InventoryManager.instance != null)
        {
            if (currentDifficulty == Difficulty.Normal)
            {
                // Normal: Vracíme inventáø do stavu pøed vstupem (ztratíš jen to, co jsi našel v dungeonu)
                InventoryManager.instance.LoadSnapshot();
            }
            else
            {
                // Hard: Pøijdeš o všechno
                InventoryManager.instance.ClearInventory();
            }
        }
    }

    public void ReturnToVillage()
    {
        SceneManager.LoadScene(villageSceneName, LoadSceneMode.Single);
    }
}