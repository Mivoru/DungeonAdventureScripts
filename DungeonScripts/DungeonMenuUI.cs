using UnityEngine;
using UnityEngine.UI;

public class DungeonMenuUI : MonoBehaviour
{
    public DungeonLevelData floor1Data; // Pøetáhni sem data Floor 1

    public void OnFloor1Clicked()
    {
        // Øekneme GameManageru, a naète tento level
        if (GameManager.instance != null)
        {
            // ZMÌNA: Voláme EnterDungeon (která øeší i Snapshoty), ne LoadLevel
            GameManager.instance.EnterDungeon(floor1Data);
        }
        else
        {
            Debug.LogError("GameManager nenalezen!");
        }

        CloseMenu();
    }

    public void CloseMenu()
    {
        // Vypne tento panel (na kterém je skript)
        gameObject.SetActive(false);
    }
}