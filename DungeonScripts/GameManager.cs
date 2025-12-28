using System.Collections.Generic;
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

    [Header("Levels Configuration")]
    // Sem v Inspectoru pøetáhneš Floor_1, Floor_2, Floor_3...
    public List<DungeonLevelData> allLevels;

    void Start()
    {
        
    }

    void Awake()
    {
        // Singleton (aby byl jen jeden manažer)
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
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Tato metoda se spustí AUTOMATICKY pokaždé, když se zmìní scéna
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("--- NOVÁ SCÉNA NAÈTENA: " + scene.name + " ---");

        // 1. Pokud jsme v Main Menu, nic nenaèítáme
        if (scene.name == "MainMenu")
        {
            return;
        }

        // 2. Pokud jsme ve Vesnici (nebo Dungeonu), zkontrolujeme Load
        if (SaveManager.instance != null)
        {
            // Používáme tu statickou promìnnou
            if (SaveManager.shouldLoadAfterSceneChange)
            {
                Debug.Log("Detekován požadavek na CONTINUE. Spouštím odpoèet...");
                StartCoroutine(DelayedLoad());
            }
            else
            {
                Debug.Log("Žádný požadavek na naètení (New Game).");
            }
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
        Debug.Log($"Zpracovávám smrt pro obtížnost: {currentDifficulty}");

        // 1. STATISTIKY (Level, XP, HP...)
        if (PlayerStats.instance != null)
        {
            if (currentDifficulty == Difficulty.Normal)
            {
                // NORMAL: Level a XP zùstávají (hráè si nechá, co nahrál)
                // Nenaèítáme snapshot statistik!
                Debug.Log("Normal Mode: XP a Level zachovány.");
            }
            else
            {
                // HARD: Vracíme se na úroveò pøed vstupem do dungeonu
                PlayerStats.instance.LoadSnapshot();
                Debug.Log("Hard Mode: Staty resetovány na stav pøed dungeonem.");
            }
        }

        // 2. INVENTÁØ (Pøedmìty)
        if (InventoryManager.instance != null)
        {
            if (currentDifficulty == Difficulty.Normal)
            {
                // NORMAL: Vracíme se do stavu pøed vstupem
                // (Zùstane to, co jsi mìl. Zmizí to, co jsi našel v dungeonu)
                InventoryManager.instance.LoadSnapshot();
                Debug.Log("Normal Mode: Inventáø obnoven ze zálohy.");
            }
            else
            {
                // HARD: Pøijdeš o všechno
                InventoryManager.instance.ClearInventory();
                Debug.Log("Hard Mode: Inventáø vymazán.");
            }
        }
    }
    System.Collections.IEnumerator DelayedLoad()
    {
        // Poèkáme, aby se stihly inicializovat ostatní skripty
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Teï volám SaveManager.LoadGame()...");
        if (SaveManager.instance != null)
        {
            SaveManager.instance.LoadGame();

            // Dùležité: Po naètení resetujeme vlajku, aby se to nenaèítalo poøád dokola
            SaveManager.shouldLoadAfterSceneChange = false;
        }
    }
    public void ReturnToVillage()
    {
        SceneManager.LoadScene(villageSceneName, LoadSceneMode.Single);
    }
}