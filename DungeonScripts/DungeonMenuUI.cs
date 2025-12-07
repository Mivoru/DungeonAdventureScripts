using UnityEngine;
using System.Collections.Generic;

public class DungeonMenuUI : MonoBehaviour
{
    [Header("References")]
    public Transform buttonsContainer; // Kam tlaèítka sypat (Content)
    public GameObject buttonPrefab;    // Vzor tlaèítka (s LevelButton skriptem)

    void OnEnable()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        // 1. Smažeme stará tlaèítka (abychom je nemìli 2x)
        foreach (Transform child in buttonsContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameManager.instance == null) return;

        List<DungeonLevelData> levels = GameManager.instance.allLevels;
        int maxUnlocked = GameManager.instance.maxUnlockedFloor;

        // 2. Projdeme seznam levelù z GameManageru
        foreach (var levelData in levels)
        {
            // Vytvoøíme tlaèítko
            GameObject newBtn = Instantiate(buttonPrefab, buttonsContainer);

            // Získáme skript a nastavíme ho
            LevelButton btnScript = newBtn.GetComponent<LevelButton>();
            if (btnScript != null)
            {
                // Zjistíme, jestli je tento level odemèený
                // (Pøedpokládáme, že Floor Index odpovídá poøadí 1, 2, 3...)
                bool isUnlocked = levelData.floorIndex <= maxUnlocked;

                btnScript.Setup(levelData, isUnlocked);
            }
        }
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
    }

}