using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;
    private string savePath;
    public static bool shouldLoadAfterSceneChange = false;    // Tajný klíè (mùžeš si vymyslet cokoliv)
    private string encryptionKey = "Moje37Super47Tajne57Heslo123";
    public int floorsUnlocked = 1;

    private string EncryptDecrypt(string text)
    {
        // Jednoduchá XOR šifra
        System.Text.StringBuilder modifiedData = new System.Text.StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            modifiedData.Append((char)(text[i] ^ encryptionKey[i % encryptionKey.Length]));
        }
        return modifiedData.ToString();
    }
    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        savePath = Application.persistentDataPath + "/savegame.json";
    }
    void Update()
    {
        // Pro jistotu kontrola, zda je pøipojená klávesnice
        if (Keyboard.current == null) return;

        // F5 = Rychlé uložení
        if (Keyboard.current.f5Key.wasPressedThisFrame)
        {
            SaveGame();
            // Zvuková/vizuální odezva by byla super, zatím jen log
            Debug.Log("Hra uložena (F5)");
        }

        // F9 = Rychlé naètení
        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            LoadGame();
            Debug.Log("Hra naètena (F9)");
        }

        // F12 = Smazat save (pro reset)
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            DeleteSave();
            Debug.Log("Save smazán (F12)");
        }
    }
    public bool HasSaveFile()
    {
        return File.Exists(savePath);
    }
    public void CheckForLoadRequest()
    {
        if (shouldLoadAfterSceneChange)
        {
            LoadGame();
            shouldLoadAfterSceneChange = false; // Reset, aby se to nenaèítalo poøád
        }
    }
    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.floorsUnlocked = floorsUnlocked;
        // 1. ZÍSKÁNÍ DAT Z PLAYER STATS
        if (PlayerStats.instance != null)
        {
            PlayerStats p = PlayerStats.instance;

            data.currentHealth = p.currentHealth;
            data.maxHealth = p.maxHealth;

            data.level = p.currentLevel;
            data.coins = p.currentCoins;
            data.xp = p.currentXP;

            data.statPoints = p.statPoints;

            data.defense = p.defense;
            data.damage = p.baseDamage;
            data.critChance = p.critChance;
            data.critDamage = p.critDamage;
            data.attackSpeed = p.attackSpeed;
            data.dashCooldownRed = p.dashCooldownRed;
            data.luck = p.luck;
            data.regeneration = p.regeneration;
        }

        if (TimeManager.instance != null)
        {
            data.daysPassed = TimeManager.instance.daysPassed;
            data.timeOfDay = TimeManager.instance.currentTime;
        }

        if (InventoryManager.instance != null)
        {
            data.inventoryItems = InventoryManager.instance.GetInventorySaveData();
        }

        // Pøevedeme data na JSON
        string json = JsonUtility.ToJson(data, true);

        // --- ŠIFROVÁNÍ (Anti-Cheat) ---
        string encryptedJson = EncryptDecrypt(json);
        // ------------------------------

        File.WriteAllText(savePath, encryptedJson);
        Debug.Log("Hra uložena (Šifrováno).");
    }

    // --- TVOJE LOAD METODA (S odšifrováním) ---
    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("Žádný save file nenalezen.");
            return;
        }

        // 1. Pøeèteme zašifrovaný text ze souboru
        string fileContent = File.ReadAllText(savePath);

        // 2. Pokusíme se ho rozšifrovat
        string json = "";
        try
        {
            json = EncryptDecrypt(fileContent);
        }
        catch
        {
            Debug.LogError("Chyba pøi dekódování savu!");
            return;
        }

        // 3. Pøevedeme JSON zpìt na data
        // Použijeme try-catch, kdyby hráè zkoušel podvrhnout poškozený soubor
        SaveData data = null;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            Debug.LogError("Save file je poškozený nebo neplatný (Anti-Cheat).");
            return;
        }
        floorsUnlocked = data.floorsUnlocked;

        // Pokud máš GameManager, pošli mu to taky:
        if (GameManager.instance != null)
        {
            // OPRAVA: V GameManageru se to jmenuje 'maxUnlockedFloor'
            GameManager.instance.maxUnlockedFloor = floorsUnlocked;
        }
        // Pokud se to povedlo, aplikujeme data:
        if (PlayerStats.instance != null)
        {
            PlayerStats p = PlayerStats.instance;

            p.currentHealth = data.currentHealth;
            p.maxHealth = data.maxHealth;

            p.currentLevel = data.level;
            p.currentCoins = data.coins;
            p.currentXP = data.xp;

            p.statPoints = data.statPoints;

            p.defense = data.defense;
            p.baseDamage = data.damage;
            p.critChance = data.critChance;
            p.critDamage = data.critDamage;
            p.attackSpeed = data.attackSpeed;
            p.dashCooldownRed = data.dashCooldownRed;
            p.luck = data.luck;
            p.regeneration = data.regeneration;

            p.UpdateHealthUI();
            if (p.levelUpUI != null) p.levelUpUI.UpdateUI();
            PlayerStats.instance.UpdateLevelUI(); 
            PlayerStats.instance.UpdateHealthUI();
            p.RecalculateRequiredXP();
        }

        if (TimeManager.instance != null)
        {
            TimeManager.instance.daysPassed = data.daysPassed;
            TimeManager.instance.currentTime = data.timeOfDay;
        }

        if (InventoryManager.instance != null)
        {
            InventoryManager.instance.LoadInventoryFromSave(data.inventoryItems);
        }

        Debug.Log("Hra naètena!");
    }
    public void UnlockFloor(int floorNumber)
    {
        // Uložíme jen pokud je to nové patro (napø. jdeme do 2. patra a máme 1)
        if (floorNumber > floorsUnlocked)
        {
            floorsUnlocked = floorNumber;
            // Mùžeme rovnou uložit hru, aby o to hráè nepøišel
            SaveGame();
            Debug.Log($"Postup uložen! Odemèeno patro: {floorsUnlocked}");
        }
    }
    public void DeleteSave()
    {
        if (File.Exists(savePath)) File.Delete(savePath);
    }
}