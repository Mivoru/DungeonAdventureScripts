using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevelData", menuName = "Dungeon/Level Data")]
public class DungeonLevelData : ScriptableObject
{
    [Header("Info")]
    public string levelName = "Floor 1";
    public int floorIndex = 1;

    [Header("Generator Settings")]
    public int numberOfRooms = 20;
    public Vector2Int minMaxRoomSize = new Vector2Int(10, 25);

    // --- VRÁCENO A UPRAVENO ---
    [Header("Boss Settings")]
    public int bossLevel = 10;           // Síla bosse (nebo každé kolikáté patro)
    public GameObject bossPrefab;        // Jaký boss to bude (Arachne, SlimeKing...)

    [Header("Enemies")]
    public List<DungeonGenerator.EnemyGroup> enemyGroups;
    public List<DungeonGenerator.SoloEnemyConfig> soloEnemies;
    public int totalGroups = 0;

    [Header("Resources")]
    public List<DungeonGenerator.ResourceConfig> resources;
}