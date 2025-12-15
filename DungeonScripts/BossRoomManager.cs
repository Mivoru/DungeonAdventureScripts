using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI; // Dùležité pro NavMeshObstacle
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
    public TileBase wallTile; // Sem dej WallBase1_RuleTile (ten s kolizí)
    public int roomSize = 25; // Polomìr od støedu

    [Header("Navigation")]
    public GameObject navMeshBarrierPrefab; // <--- SEM PØETÁHNI TU BARIÉRU Z KROKU 1

    private bool isLocked = false;
    private Tilemap wallsTilemap;

    // Seznamy pro úklid po boji
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
            StartCoroutine(LockRoomSequence());
        }
    }

    IEnumerator LockRoomSequence()
    {
        isLocked = true;

        // 1. Èekáme, až hráè vbìhne dovnitø
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ZAMYKÁM MÍSTNOST!");

        // 2. Zrušíme možnost útìku pøes ESC
        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = false;

        // 3. VYGENERUJEME ZDI A BARIÉRY
        if (wallsTilemap != null && wallTile != null)
        {
            Vector3Int center = wallsTilemap.WorldToCell(transform.position);
            int r = roomSize;

            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    // Øešíme jen obvod
                    if (Mathf.Abs(x) == r || Mathf.Abs(y) == r)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, 0);

                        // Pokud tam není zeï (je to vchod), zazdíme to
                        if (!wallsTilemap.HasTile(pos))
                        {
                            // A) Grafická a fyzická zeï (pro hráèe)
                            wallsTilemap.SetTile(pos, wallTile);
                            addedWalls.Add(pos);

                            // B) NavMesh Bariéra (pro monstra)
                            if (navMeshBarrierPrefab != null)
                            {
                                // Pøevedeme grid pozici na svìtovou a vycentrujeme (+0.5)
                                Vector3 worldPos = wallsTilemap.CellToWorld(pos) + new Vector3(0.5f, 0.5f, 0);
                                GameObject barrier = Instantiate(navMeshBarrierPrefab, worldPos, Quaternion.identity);
                                activeBarriers.Add(barrier);
                            }
                        }
                    }
                }
            }
        }

        // 4. Spawn Bosse
        if (activeBoss == null && bossPrefab != null)
        {
            activeBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
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
        isLocked = false;
        Debug.Log("MÍSTNOST ODEMÈENA!");

        // 1. Povolit ESC
        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = true;

        // 2. Zbourat zdi (grafiku)
        if (wallsTilemap != null)
        {
            foreach (Vector3Int pos in addedWalls)
            {
                wallsTilemap.SetTile(pos, null);
            }
        }

        // 3. Odstranit bariéry (aby monstra mohla zase chodit)
        foreach (GameObject barrier in activeBarriers)
        {
            Destroy(barrier);
        }
        activeBarriers.Clear();

        // 4. Spawn Portál
        if (exitPortalPrefab != null && portalSpawnPoint != null)
        {
            Instantiate(exitPortalPrefab, portalSpawnPoint.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}