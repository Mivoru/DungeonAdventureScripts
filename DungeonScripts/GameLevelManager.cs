using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class FloorSettings
{
    public int floorNumber;
    public List<EnemyData> availableEnemies; // Na patøe 1 jen Slime, na patøe 5 i Necromancer
    public int roomCount;
}

public class GameLevelManager : MonoBehaviour
{
    public int currentFloor = 1;
    public List<FloorSettings> floors;

    public void NextLevel()
    {
        currentFloor++;
        // Zvýšíme obtížnost
        // Zavoláme DungeonGenerator.Generate(floors[currentFloor])
    }
}