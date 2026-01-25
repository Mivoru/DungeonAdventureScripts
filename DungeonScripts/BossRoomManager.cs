using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class BossRoomManager : MonoBehaviour
{
    [Header("Setup")]
    public GameObject exitPortalPrefab;
    public Transform bossSpawnPoint;
    public Transform portalSpawnPoint;

    [Header("Boss Fallback")]
    public GameObject defaultBossPrefab; // Záložní boss (kdyby data chybìla)
    private GameObject activeBoss;

    [Header("Room Locking")]
    public TileBase wallTile;
    public int roomSize = 25;
    public GameObject navMeshBarrierPrefab;

    private bool isLocked = false;
    private Tilemap wallsTilemap;
    private List<Vector3Int> addedWalls = new List<Vector3Int>();
    private List<GameObject> activeBarriers = new List<GameObject>();
    private bool bossSpawned = false;

    void Start()
    {
        GameObject wallsObj = GameObject.Find("Walls");
        if (wallsObj != null) wallsTilemap = wallsObj.GetComponent<Tilemap>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLocked && other.CompareTag("Player"))
        {
            isLocked = true;
            StartCoroutine(LockRoomSequence());
        }
    }

    IEnumerator LockRoomSequence()
    {
        yield return new WaitForSeconds(1.0f);

        // --- ZAMÈENÍ MÍSTNOSTI ---
        Debug.Log("ZAMYKÁM MÍSTNOST!");
        if (AudioManager.instance != null) AudioManager.instance.PlayRandomBossTheme();

        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = false;

        LockWalls(); // (Vytáhl jsem to do metody dole pro pøehlednost)

        // --- SPAWN BOSSE ---
        if (activeBoss == null)
        {
            GameObject prefabToSpawn = defaultBossPrefab; // Výchozí je záloha
            int targetLevel = 10;

            // 1. ÈTEME DATA Z LEVELU (Stejnì jako generátor)
            if (GameManager.instance != null && GameManager.instance.currentLevelData != null)
            {
                DungeonLevelData data = GameManager.instance.currentLevelData;

                // Má tento level nastaveného specifického bosse?
                if (data.bossPrefab != null)
                {
                    prefabToSpawn = data.bossPrefab;
                }

                // Jaký má mít level?
                targetLevel = data.bossLevel;
            }

            // 2. Samotný Spawn
            if (prefabToSpawn != null)
            {
                activeBoss = Instantiate(prefabToSpawn, bossSpawnPoint.position, Quaternion.identity);

                // 3. Nastavení síly (Levelu)
                EnemyStats stats = activeBoss.GetComponent<EnemyStats>();
                if (stats != null)
                {
                    stats.SetLevel(targetLevel);
                    Debug.Log($"Boss '{prefabToSpawn.name}' spawnut (Level {targetLevel}) z LevelData.");
                }

                bossSpawned = true;
            }
            else
            {
                Debug.LogError("CHYBA: BossRoomManager nemá co spawnout! Chybí prefab v LevelData i v defaultBossPrefab.");
                UnlockRoom(); // Odemkneme, aby se hráè nezasekl
            }
        }
    }

    void Update()
    {
        // Pokud byl boss spawnut a teï už je null (zemøel) -> Odemknout
        if (isLocked && bossSpawned && activeBoss == null)
        {
            UnlockRoom();
        }
    }

    // --- POMOCNÉ METODY ---

    void LockWalls()
    {
        if (wallsTilemap != null && wallTile != null)
        {
            Vector3Int center = wallsTilemap.WorldToCell(transform.position);
            int r = roomSize;
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    if (Mathf.Abs(x) == r || Mathf.Abs(y) == r)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, 0);
                        if (!wallsTilemap.HasTile(pos))
                        {
                            wallsTilemap.SetTile(pos, wallTile);
                            addedWalls.Add(pos);
                            if (navMeshBarrierPrefab != null)
                            {
                                Vector3 worldPos = wallsTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);
                                activeBarriers.Add(Instantiate(navMeshBarrierPrefab, worldPos, Quaternion.identity));
                            }
                        }
                    }
                }
            }
        }
    }

    void UnlockRoom()
    {
        Debug.Log("MÍSTNOST ODEMÈENA!");
        if (AudioManager.instance != null) AudioManager.instance.PlayMusic("DungeonAmbient1");

        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = true;

        if (wallsTilemap != null)
        {
            foreach (Vector3Int pos in addedWalls) wallsTilemap.SetTile(pos, null);
        }

        foreach (GameObject barrier in activeBarriers) Destroy(barrier);
        activeBarriers.Clear();

        if (exitPortalPrefab != null && portalSpawnPoint != null)
        {
            Instantiate(exitPortalPrefab, portalSpawnPoint.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}