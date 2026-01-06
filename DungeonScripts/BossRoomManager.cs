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

    // ZMÃNA: Tady uû nenÌ û·dn· hudba, vöe ¯eöÌ AudioManager centr·lnÏ.

    [Header("Room Locking (Gen)")]
    public TileBase wallTile;
    public int roomSize = 25;

    [Header("Navigation")]
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
            StartCoroutine(LockRoomSequence());
        }
    }

    IEnumerator LockRoomSequence()
    {
        isLocked = true;
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ZAMYK¡M MÕSTNOST!");

        // --- ZMÃNA: ÿekneme AudioManageru, aù nÏco vybere ---
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayRandomBossTheme();
        }
        // ----------------------------------------------------

        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = false;

        // ZDI A BARI…RY
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

        // SPAWN BOSSE
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
        Debug.Log("MÕSTNOST ODEM»ENA!");

        // N¡VRAT K AMBIENTU
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