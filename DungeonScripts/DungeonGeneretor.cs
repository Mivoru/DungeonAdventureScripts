using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using NavMeshPlus.Components;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Room Settings")]
    public int minRoomSize = 10;
    public int maxRoomSize = 25;
    public int numberOfRooms = 20;

    [Header("Corridor Settings")]
    public int minCorridorWidth = 3;
    public int maxCorridorWidth = 5;
    public int minCorridorLength = 8;
    public int maxCorridorLength = 16;

    [Header("Wall Height Logic")]
    public int verticalPadding = 6;

    [Header("Boss Room")]
    public Vector2Int bossRoomSize = new Vector2Int(50, 50);
    public int bossLevel = 10;

    [Header("High Walls Settings")]
    public Tilemap WallDecorMap;
    public TileBase wallMid;
    public TileBase wallHigh;
    public TileBase wallRoof;

    [Header("Shadow Settings")]
    public Tilemap ShadowMap;
    public TileBase floorShadow;
    public TileBase wallShadow;

    [Header("Spawn Room")]
    public Vector2Int spawnRoomSize = new Vector2Int(10, 10);

    [Header("Boss Room Setup")]
    public GameObject bossRoomControllerPrefab;

    // --- PÿID¡NO: KONFIGURACE PRO RUDY ---
    [System.Serializable]
    public class ResourceConfig
    {
        public string name = "Ore";
        public GameObject prefab;
        public int minCount = 5;
        public int maxCount = 10;
    }

    [Header("Resources Generation")]
    public List<ResourceConfig> resourcesToSpawn;
    // --------------------------------------

    [System.Serializable]
    public class EnemyGroup
    {
        public string groupName = "Patrol";
        public List<GameObject> enemiesInGroup;
        public int minLevel = 1;
        public int maxLevel = 3;
    }

    [Header("Group Spawning Settings")]
    public List<EnemyGroup> enemyGroups;
    public int totalGroupsToSpawn = 5;
    public float safeZoneRadius = 5f;

    [System.Serializable]
    public class SoloEnemyConfig
    {
        public string name = "Enemy Type";
        public GameObject prefab;
        public int count;
        public int minLevel = 1;
        public int maxLevel = 3;
    }

    [Header("Solo Spawning Settings")]
    public List<SoloEnemyConfig> soloEnemiesToSpawn;

    [HideInInspector] public int enemyCount = 0;

    [Header("References")]
    public Tilemap Ground;
    public Tilemap Walls;
    public TileBase FloorTile;
    public TileBase WallTile;
    public GameObject navMeshObject;

    [System.Serializable]
    public class Room
    {
        public RectInt bounds;
        public Vector3Int center => new Vector3Int(bounds.x + bounds.width / 2, bounds.y + bounds.height / 2, 0);
        public bool hasEnemies = false;
    }

    private List<Room> rooms = new List<Room>();

    // Pouze 4 smÏry (û·dnÈ diagon·ly) pro hezËÌ zdi
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Sever
        new Vector2Int(1, 1),   // Severo-V˝chod
        new Vector2Int(1, 0),   // V˝chod
        new Vector2Int(1, -1),  // Jiho-V˝chod
        new Vector2Int(0, -1),  // Jih
        new Vector2Int(-1, -1), // Jiho-Z·pad
        new Vector2Int(-1, 0),  // Z·pad
        new Vector2Int(-1, 1)   // Severo-Z·pad
    };

    void Start()
    {
        // 1. ZkusÌme najÌt äÈfa (GameManager)
        if (GameManager.instance != null && GameManager.instance.currentLevelData != null)
        {
            Debug.Log("NaËÌt·m data z GameManageru...");
            LoadData(GameManager.instance.currentLevelData);
        }
        else
        {
            Debug.LogWarning("Hraju jen testovacÌ scÈnu (bez GameManageru). PouûÌv·m nastavenÌ z Inspectoru.");
        }

        // 2. ZaËneme stavÏt
        GenerateDungeon();
    }

    void LoadData(DungeonLevelData data)
    {
        // P¯epÌöeme nastavenÌ gener·toru hodnotami z Dat
        minRoomSize = data.minMaxRoomSize.x;
        maxRoomSize = data.minMaxRoomSize.y;
        numberOfRooms = data.numberOfRooms;

        enemyGroups = data.enemyGroups;
        soloEnemiesToSpawn = data.soloEnemies;
        totalGroupsToSpawn = data.totalGroups;

        bossLevel = data.bossLevel;
        resourcesToSpawn = data.resources;
    }

    void GenerateDungeon()
    {
        // 1. VyËiötÏnÌ
        Ground.ClearAllTiles();
        Walls.ClearAllTiles();
        if (WallDecorMap != null) WallDecorMap.ClearAllTiles();

        rooms.Clear();

        foreach (var enemy in GameObject.FindGameObjectsWithTag("Enemy")) Destroy(enemy);
        foreach (var res in GameObject.FindGameObjectsWithTag("Resource")) Destroy(res); // Pokud majÌ tag Resource
        // 2. StartovnÌ mÌstnost
        CreateRoom(Vector2Int.zero, UnityEngine.Random.Range(minRoomSize, maxRoomSize), UnityEngine.Random.Range(minRoomSize, maxRoomSize));

        // 3. Generov·nÌ dalöÌch mÌstnostÌ
        int attempts = 0;
        while (rooms.Count < numberOfRooms && attempts < 1000)
        {
            attempts++;
            if (TryGenerateRoom()) attempts = 0;
        }

        // 4. Boss Room
        CreateBossRoom();

        // 5. ⁄prava terÈnu (Aby se veöly zdi)
        PostProcessFloor();

        // 6. Vytvo¯enÌ zdÌ a dekoracÌ
        CreateWalls();     // Zalije okolÌ st¯echou
        BuildHighWalls();  // Vytvo¯Ì fas·dy

        CreateWalls();
        BuildHighWalls();

        AddShadows(); // <--- NOV… VOL¡NÕ

        StartCoroutine(FinalizeLevel());

    }

    // --- GENERACE MÕSTNOSTÕ ---

    bool TryGenerateRoom()
    {
        Room sourceRoom = rooms[UnityEngine.Random.Range(0, rooms.Count)];
        Vector2Int direction = directions[UnityEngine.Random.Range(0, directions.Length)];

        int corridorWidth = Mathf.Max(2, UnityEngine.Random.Range(minCorridorWidth, maxCorridorWidth));
        int corridorLength = UnityEngine.Random.Range(minCorridorLength, maxCorridorLength);

        // Pro vertik·lnÌ chodby p¯id·me mÌsto pro zdi
        if (Mathf.Abs(direction.y) > 0)
        {
            corridorLength += verticalPadding;
        }

        int newRoomW = UnityEngine.Random.Range(minRoomSize, maxRoomSize);
        int newRoomH = UnityEngine.Random.Range(minRoomSize, maxRoomSize);

        Vector2Int startPos = new Vector2Int(sourceRoom.center.x, sourceRoom.center.y);
        int offsetFromCenter = (Mathf.Max(sourceRoom.bounds.width, sourceRoom.bounds.height) / 2) + (Mathf.Max(newRoomW, newRoomH) / 2);
        Vector2Int corridorEnd = startPos + (direction * (corridorLength + offsetFromCenter));

        RectInt newRoomRect = new RectInt(corridorEnd.x - newRoomW / 2, corridorEnd.y - newRoomH / 2, newRoomW, newRoomH);

        // Kontrola kolize s rezervou pro zdi
        RectInt paddedRect = new RectInt(
            newRoomRect.x - 2,
            newRoomRect.y - 2,
            newRoomRect.width + 4,
            newRoomRect.height + 4 + verticalPadding
        );

        foreach (var room in rooms)
        {
            if (room.bounds.Overlaps(paddedRect)) return false;
        }

        PaintCorridor(sourceRoom.center, new Vector3Int(corridorEnd.x, corridorEnd.y, 0), corridorWidth);
        CreateRoom(new Vector2Int(corridorEnd.x, corridorEnd.y), newRoomW, newRoomH);

        return true;
    }

    void CreateRoom(Vector2Int center, int w, int h)
    {
        RectInt rect = new RectInt(center.x - w / 2, center.y - h / 2, w, h);
        Room newRoom = new Room { bounds = rect };
        rooms.Add(newRoom);

        for (int x = rect.x; x < rect.xMax; x++)
        {
            for (int y = rect.y; y < rect.yMax; y++)
            {
                Ground.SetTile(new Vector3Int(x, y, 0), FloorTile);
            }
        }
    }

    void PaintCorridor(Vector3Int start, Vector3Int end, int width)
    {
        // P¯evedeme na Vector3 pro plynul˝ v˝poËet smÏru
        Vector3 startPos = (Vector3)start;
        Vector3 endPos = (Vector3)end;

        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        // Jdeme po Ë·¯e po mal˝ch krocÌch (0.5), abychom nevynechali û·dnou dlaûdici
        for (float i = 0; i <= distance; i += 0.5f)
        {
            Vector3 point = startPos + direction * i;
            Vector3Int gridPoint = Vector3Int.RoundToInt(point);

            // VykreslÌme "ötÏtec" (Ëtverec) o velikosti width
            int halfWidth = width / 2;

            // Korekce pro sud· ËÌsla, aby to bylo vycentrovanÈ
            // (DoporuËuji pouûÌvat lichÈ öÌ¯ky chodeb: 3 nebo 5)
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                for (int y = -halfWidth; y <= halfWidth; y++)
                {
                    Ground.SetTile(gridPoint + new Vector3Int(x, y, 0), FloorTile);
                }
            }
        }
    }

    void CreateBossRoom()
    {
        if (rooms.Count == 0) return;

        // 1. Najdeme mÌsto pro Boss Room (nejd·l od startu)
        Room furthestRoom = rooms.OrderByDescending(r => Vector3.Distance(Vector3.zero, r.center)).First();

        // --- DŸLEéIT¡ OPRAVA: SMAéEME TU STAROU MÕSTNOST ---
        // Aby ji gener·tor nep¯·tel uû nevidÏl a nespawnoval do nÌ moby.
        rooms.Remove(furthestRoom);

        int w = bossRoomSize.x;
        int h = bossRoomSize.y;
        Vector3Int center = furthestRoom.center;

        // 2. Vytvo¯Ìme obdÈlnÌk Boss mÌstnosti
        RectInt bossRect = new RectInt(center.x - w / 2, center.y - h / 2, w, h);

        // 3. P¯id·me Boss Room do seznamu (teÔ bude opravdu poslednÌ)
        rooms.Add(new Room { bounds = bossRect });

        // 4. VykreslÌme podlahu
        for (int x = bossRect.x; x < bossRect.xMax; x++)
        {
            for (int y = bossRect.y; y < bossRect.yMax; y++)
            {
                Ground.SetTile(new Vector3Int(x, y, 0), FloorTile);
            }
        }

        // 5. VypoËÌt·me st¯ed pro spawn manageru
        Vector3 bossRoomCenter = new Vector3(bossRect.center.x, bossRect.center.y, 0);

        // 6. SPAWN MANAGERU (MÌsto port·lu)
        if (bossRoomControllerPrefab != null)
        {
            Instantiate(bossRoomControllerPrefab, bossRoomCenter, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("ChybÌ BossRoomControllerPrefab v Inspectoru DungeonGeneratoru!");
        }
    }
    // --- OPRAVY TER…NU A ZDI ---

    void CreateWalls()
    {
        // VyplnÌme vöe okolo St¯echou
        Ground.CompressBounds();
        BoundsInt bounds = Ground.cellBounds;
        int padding = 30;

        for (int x = bounds.xMin - padding; x <= bounds.xMax + padding; x++)
        {
            for (int y = bounds.yMin - padding; y <= bounds.yMax + padding; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                // Kde nenÌ podlaha, tam je st¯echa (ve vrstvÏ Walls kv˘li kolizi na okraji)
                if (Ground.GetTile(pos) == null)
                {
                    Walls.SetTile(pos, wallRoof);
                }
            }
        }
    }

    void BuildHighWalls()
    {
        if (WallDecorMap == null) return;
        WallDecorMap.ClearAllTiles();

        // Projdeme hranu podlahy
        foreach (var pos in Ground.cellBounds.allPositionsWithin)
        {
            if (Ground.GetTile(pos) != null)
            {
                Vector3Int posNorth1 = pos + new Vector3Int(0, 1, 0);

                // Pokud nad podlahou je st¯echa -> stavÌme fas·du
                if (Ground.GetTile(posNorth1) == null)
                {
                    // 1. Spodek (KoliznÌ)
                    Walls.SetTile(posNorth1, WallTile);

                    // 2. St¯ed (Dekorace)
                    Vector3Int posNorth2 = pos + new Vector3Int(0, 2, 0);
                    WallDecorMap.SetTile(posNorth2, wallMid);

                    // 3. Vröek (Dekorace)
                    Vector3Int posNorth3 = pos + new Vector3Int(0, 3, 0);
                    WallDecorMap.SetTile(posNorth3, wallHigh);

                    // 4. St¯echa (VyËistit dekoraci, aby byla vidÏt st¯echa z Walls)
                    Vector3Int posNorth4 = pos + new Vector3Int(0, 4, 0);
                    WallDecorMap.SetTile(posNorth4, null);
                }
            }
        }
    }
    void PostProcessFloor()
    {
        // Opakujeme 3x pro d˘kladnÈ vyhlazenÌ
        for (int i = 0; i < 3; i++)
        {
            Ground.CompressBounds();
            BoundsInt bounds = Ground.cellBounds;
            List<Vector3Int> tilesToChange = new List<Vector3Int>();

            // Projdeme oblast s rezervou
            for (int x = bounds.xMin - 2; x <= bounds.xMax + 2; x++)
            {
                for (int y = bounds.yMin - 2; y <= bounds.yMax + 2; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    // Pokud je to ZEœ (nenÌ podlaha)
                    if (Ground.GetTile(pos) == null)
                    {
                        // Zkontrolujeme sousedy v k¯Ìûi (N, S, E, W)
                        int floorNeighbors = 0;
                        if (Ground.GetTile(pos + Vector3Int.up) != null) floorNeighbors++;
                        if (Ground.GetTile(pos + Vector3Int.down) != null) floorNeighbors++;
                        if (Ground.GetTile(pos + Vector3Int.left) != null) floorNeighbors++;
                        if (Ground.GetTile(pos + Vector3Int.right) != null) floorNeighbors++;

                        // PRAVIDLO 1: "äpiËka" nebo "Tenk· zeÔ"
                        // Pokud je zeÔ obklopen· podlahou ze 3 nebo 4 stran, je moc tenk· -> ZBOURAT
                        if (floorNeighbors >= 3)
                        {
                            tilesToChange.Add(pos);
                        }

                        // PRAVIDLO 2: Vertik·lnÌ tlouöùka (pro vysokÈ zdi)
                        // Pokud je pode mnou podlaha (jsem fas·da), ale nade mnou je hned zase podlaha (jsem jen 1 blok tlust˝)
                        // Tak to zbourej.
                        bool floorBelow = Ground.GetTile(pos + Vector3Int.down) != null;
                        bool floorAbove1 = Ground.GetTile(pos + new Vector3Int(0, 1, 0)) != null;
                        bool floorAbove2 = Ground.GetTile(pos + new Vector3Int(0, 2, 0)) != null;
                        bool floorAbove3 = Ground.GetTile(pos + new Vector3Int(0, 3, 0)) != null;

                        if (floorBelow)
                        {
                            // Pokud zeÔ nepokraËuje alespoÚ 3 bloky nahoru (aby se veöla fas·da)
                            if (floorAbove1 || floorAbove2 || floorAbove3)
                            {
                                tilesToChange.Add(pos);
                            }
                        }
                    }
                }
            }

            // Aplikujeme zmÏny (zmÏnÌme na podlahu)
            foreach (var pos in tilesToChange)
            {
                Ground.SetTile(pos, FloorTile);
            }
        }
    }

    // --- POMOCN¡ METODA ---
    int CountFloorNeighbors(Vector3Int pos)
    {
        int count = 0;
        // Projdeme m¯Ìûku 3x3 kolem bodu
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // S·m sebe nepoËÌt·m

                // Pokud je na sousednÌ pozici podlaha, p¯iËteme bod
                if (Ground.GetTile(pos + new Vector3Int(x, y, 0)) != null)
                {
                    count++;
                }
            }
        }
        return count;
    }

    void AddShadows()
    {
        if (ShadowMap == null) return;
        ShadowMap.ClearAllTiles();

        // Projdeme vöechny dlaûdice PODLAHY
        foreach (var pos in Ground.cellBounds.allPositionsWithin)
        {
            if (Ground.GetTile(pos) != null) // Pokud tu je podlaha
            {
                // --- 1. POZADÕ POD ZDMI (2 bloky do vöech stran) ---
                // Projdeme okolÌ +-2 bloky
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        // S·m sebe (0,0) p¯eskakujeme, tam bude podlaha
                        if (x == 0 && y == 0) continue;

                        Vector3Int neighborPos = pos + new Vector3Int(x, y, 0);

                        // Pokud na sousednÌ pozici NENÕ podlaha (je to zeÔ nebo pr·zdno)
                        if (Ground.GetTile(neighborPos) == null)
                        {
                            // D·me tam Ëernou v˝plÚ (WallShadow)
                            // TÌm vytvo¯Ìme "lÌmec" tmy kolem celÈ mÌstnosti
                            ShadowMap.SetTile(neighborPos, wallShadow);
                        }
                    }
                }

                // --- 2. STÕN NA PODLAZE (SevernÌ a Z·padnÌ okraj) ---
                // Zkontrolujeme p¯ÌmÈ sousedy
                Vector3Int posNorth = pos + new Vector3Int(0, 1, 0);
                Vector3Int posWest = pos + new Vector3Int(-1, 0, 0);
                Vector3Int posNorthWest = pos + new Vector3Int(-1, 1, 0); // Roh

                bool wallNorth = Ground.GetTile(posNorth) == null;
                bool wallWest = Ground.GetTile(posWest) == null;
                bool wallNorthWest = Ground.GetTile(posNorthWest) == null;

                // Pokud je zeÔ naho¯e NEBO vlevo NEBO v rohu (pro hezkÈ spojenÌ)
                if (wallNorth || wallWest || (wallNorth && wallWest) || wallNorthWest)
                {
                    // Na TUTO dlaûdici podlahy poloûÌme polopr˘hledn˝ stÌn
                    // Protoûe je to Rule Tile, s·m si vy¯eöÌ, jak m· vypadat (roh vs rovn˝)
                    ShadowMap.SetTile(pos, floorShadow);
                }
            }
        }
    }

    // --- FINALIZE & SPAWN ---

    IEnumerator FinalizeLevel()
    {
        yield return new WaitForEndOfFrame();
        Ground.RefreshAllTiles();
        Walls.RefreshAllTiles();
        if (WallDecorMap != null) WallDecorMap.RefreshAllTiles();

        yield return new WaitForSeconds(0.2f);

        Debug.Log("FinalizeLevel: Building NavMesh...");

        var surface = navMeshObject.GetComponent<NavMeshSurface>();
        if (surface != null)
        {
            surface.RemoveData();
            surface.BuildNavMesh();

            var triang = NavMesh.CalculateTriangulation();
            if (triang.vertices.Length > 0)
            {
                Debug.Log($" NavMesh OK! Vertices: {triang.vertices.Length}");
                foreach (var node in GameObject.FindObjectsOfType<ResourceNode>()) Destroy(node.gameObject);
                MovePlayerToStart();
                SpawnEnemyGroups();
                SpawnSoloEnemies();
                SpawnResources();
            }
            else
            {
                Debug.LogError(" NavMesh m· 0 vrchol˘.");
            }
        }
    }

    void SetupEnemy(GameObject enemyObj, int minLvl, int maxLvl)
    {
        EnemyStats stats = enemyObj.GetComponent<EnemyStats>();
        if (stats != null)
        {
            int randomLevel = UnityEngine.Random.Range(minLvl, maxLvl + 1);
            stats.SetLevel(randomLevel);
        }
    }

    void SpawnEnemyGroups()
    {
        int spawnedGroups = 0;
        int attempts = 0;
        while (spawnedGroups < totalGroupsToSpawn && attempts < 1000)
        {
            attempts++;
            Room r = rooms[UnityEngine.Random.Range(1, rooms.Count - 1)];
            if (Vector3.Distance(Vector3.zero, r.center) < safeZoneRadius) continue;
            if (r.hasEnemies) continue;
            if (enemyGroups.Count == 0) break;

            EnemyGroup group = enemyGroups[UnityEngine.Random.Range(0, enemyGroups.Count)];

            foreach (GameObject enemyPrefab in group.enemiesInGroup)
            {
                float randomX = UnityEngine.Random.Range(r.bounds.xMin + 2, r.bounds.xMax - 2);
                float randomY = UnityEngine.Random.Range(r.bounds.yMin + 2, r.bounds.yMax - 2);
                Vector3 spawnPos = new Vector3(randomX, randomY, 0);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    GameObject newEnemy = Instantiate(enemyPrefab, hit.position, Quaternion.identity);
                    SetupEnemy(newEnemy, group.minLevel, group.maxLevel);
                }
            }
            r.hasEnemies = true;
            spawnedGroups++;
        }
    }

    void SpawnSoloEnemies()
    {
        foreach (var config in soloEnemiesToSpawn)
        {
            if (config.prefab == null || config.count <= 0) continue;
            int spawnedCount = 0;
            int attempts = 0;
            while (spawnedCount < config.count && attempts < 1000)
            {
                attempts++;
                Room r = rooms[UnityEngine.Random.Range(1, rooms.Count - 1)];
                if (Vector3.Distance(Vector3.zero, r.center) < safeZoneRadius) continue;
                float randomX = UnityEngine.Random.Range(r.bounds.xMin + 2, r.bounds.xMax - 2);
                float randomY = UnityEngine.Random.Range(r.bounds.yMin + 2, r.bounds.yMax - 2);
                Vector3 spawnPos = new Vector3(randomX, randomY, 0);
                NavMeshHit hit;
                if (NavMesh.SamplePosition(spawnPos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    GameObject newEnemy = Instantiate(config.prefab, hit.position, Quaternion.identity);
                    SetupEnemy(newEnemy, config.minLevel, config.maxLevel);
                    spawnedCount++;
                }
            }
        }
    }

    void SpawnResources()
    {
        if (resourcesToSpawn == null) return;

        foreach (var config in resourcesToSpawn)
        {
            if (config.prefab == null) continue;

            int count = UnityEngine.Random.Range(config.minCount, config.maxCount + 1);
            int spawned = 0;
            int attempts = 0;

            while (spawned < count && attempts < 500)
            {
                attempts++;
                // Vybereme n·hodnou mÌstnost (kromÏ startu a bosse)
                if (rooms.Count <= 2) break;
                Room r = rooms[UnityEngine.Random.Range(1, rooms.Count - 1)];

                // N·hodn· pozice
                float randomX = UnityEngine.Random.Range(r.bounds.xMin + 2, r.bounds.xMax - 2);
                float randomY = UnityEngine.Random.Range(r.bounds.yMin + 2, r.bounds.yMax - 2);
                Vector3 spawnPos = new Vector3(randomX, randomY, 0);

                // Kontrola kolize (aby to nebylo ve zdi nebo v jinÈ rudÏ)
                // PouûÌv·me mal˝ kruh
                if (Physics2D.OverlapCircle(spawnPos, 0.5f) == null)
                {
                    Instantiate(config.prefab, spawnPos, Quaternion.identity);
                    spawned++;
                }
            }
        }
    }

    void MovePlayerToStart()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        // rooms[0] je naöe Spawn Room (protoûe ji vytv·¯Ìme jako prvnÌ)
        if (player != null && rooms.Count > 0)
        {
            // VypoËÌt·me st¯ed (+0.5 aby to bylo uprost¯ed dlaûdice)
            Vector3 startPos = new Vector3(rooms[0].center.x + 0.5f, rooms[0].center.y + 0.5f, 0);

            // Pouûijeme NavMesh.SamplePosition, abychom mÏli jistotu, ûe hr·Ë nebude ve zdi
            NavMeshHit hit;
            // Hled·me v okruhu 5 metr˘ od st¯edu mÌstnosti
            if (NavMesh.SamplePosition(startPos, out hit, 5.0f, NavMesh.AllAreas))
            {
                player.transform.position = hit.position;
            }
            else
            {
                player.transform.position = startPos; // Z·loha, kdyby NavMesh selhal
            }

            // Resetujeme fyziku, aby hr·Ë neodletÏl setrvaËnostÌ
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}