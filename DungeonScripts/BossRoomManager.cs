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

    [Header("Boss")]
    public GameObject bossPrefab;
    private GameObject activeBoss;

    [Header("Room Locking (Gen)")]
    public TileBase wallTile;
    public int roomSize = 25;

    [Header("Navigation")]
    public GameObject navMeshBarrierPrefab;

    private bool isLocked = false; // Slouží zároveò jako pojistka proti dvojitému spuštìní
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
        // Pokud už je zamèeno, nic nedìlej.
        if (!isLocked && other.CompareTag("Player"))
        {
            // 1. DÙLEŽITÉ: Zamkneme to HNED TEÏ, ne až v Coroutine.
            // Tím zabráníme tomu, aby se Boss spawnul 2x, když hráè rychle projde triggerem.
            isLocked = true;
            StartCoroutine(LockRoomSequence());
        }
    }

    IEnumerator LockRoomSequence()
    {
        // (isLocked = true už jsme nastavili nahoøe)

        yield return new WaitForSeconds(1.0f);

        Debug.Log("ZAMYKÁM MÍSTNOST!");

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayRandomBossTheme();
        }

        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = false;

        // ZDI A BARIÉRY
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
                                GameObject barrier = Instantiate(navMeshBarrierPrefab, worldPos, Quaternion.identity);
                                activeBarriers.Add(barrier);
                            }
                        }
                    }
                }
            }
        }

        // SPAWN BOSSE A NASTAVENÍ LEVELU
        if (activeBoss == null && bossPrefab != null)
        {
            activeBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);

            // --- TADY JE TA OPRAVA LEVELU ---
            // 1. Zjistíme, jaký level má Boss mít (z GameManageru -> Floor Data)
            int targetLevel = 10; // Defaultní hodnota pro jistotu

            if (GameManager.instance != null && GameManager.instance.currentLevelData != null)
            {
                targetLevel = GameManager.instance.currentLevelData.bossLevel;
            }
            // Pokud nemáme GameManager (testování), zkusíme to vytáhnout z Generátoru
            else
            {
                var gen = FindFirstObjectByType<DungeonGenerator>();
                if (gen != null) targetLevel = gen.bossLevel;
            }

            // 2. Aplikujeme level na Bosse
            EnemyStats stats = activeBoss.GetComponent<EnemyStats>();
            if (stats != null)
            {
                stats.SetLevel(targetLevel); // Tohle pøepoèítá životy a damage
                Debug.Log($"Boss spawnut! Level nastaven na: {targetLevel}");
            }
            // --------------------------------

            bossSpawned = true;
        }
    }

    void Update()
    {
        if (isLocked && bossSpawned && activeBoss == null)
        {
            UnlockRoom();
        }
    }

    void UnlockRoom()
    {
        // isLocked = false nastavíme až úplnì na konci, nebo necháme true, 
        // protože tato instance Manageru se stejnì znièí.
        Debug.Log("MÍSTNOST ODEMÈENA!");

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayMusic("DungeonAmbient1");
        }

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