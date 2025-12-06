using UnityEngine;
using UnityEngine.Tilemaps;
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
    public int roomSize = 25; // Polomìr od støedu (polovina z 50)

    private bool isLocked = false;
    private Tilemap wallsTilemap;
    private List<Vector3Int> addedWalls = new List<Vector3Int>(); // Pamatujeme si, co jsme postavili
    private bool bossSpawned = false;

    void Start()
    {
        // Najdeme Tilemapu se zdmi (podle názvu ve scénì)
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
        isLocked = true; // Zamkneme logiku hned

        // 1. Poèkáme 1 vteøinu (aby hráè stihl vbìhnout dovnitø)
        yield return new WaitForSeconds(1.0f);

        Debug.Log("ZAMYKÁM MÍSTNOST!");

        // 2. Zrušíme ESC
        EscapeToVillage escapeScript = FindFirstObjectByType<EscapeToVillage>();
        if (escapeScript != null) escapeScript.enabled = false;

        // 3. VYGENERUJEME ZDI PO OBVODU
        if (wallsTilemap != null && wallTile != null)
        {
            Vector3Int center = wallsTilemap.WorldToCell(transform.position);
            int r = roomSize;

            // Projdeme ètvercový obvod místnosti
            for (int x = -r; x <= r; x++)
            {
                for (int y = -r; y <= r; y++)
                {
                    // Zajímá nás jen hrana (obvod)
                    if (Mathf.Abs(x) == r || Mathf.Abs(y) == r)
                    {
                        Vector3Int pos = center + new Vector3Int(x, y, 0);

                        // Pokud na hranì NENÍ zeï (je tam díra/vchod)
                        if (!wallsTilemap.HasTile(pos))
                        {
                            wallsTilemap.SetTile(pos, wallTile); // Zazdíme to
                            addedWalls.Add(pos); // Zapamatujeme si to pro odemèení
                        }
                    }
                }
            }
        }

        // 4. Spawn Bosse
        if (activeBoss == null && bossPrefab != null)
        {
            activeBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            bossSpawned = true; // <--- BOSS JE NA SCÉNÌ!
        }
    }

    void Update()
    {
        // Kontrola smrti bosse
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

        // 2. Zbourat zdi (které jsme postavili)
        if (wallsTilemap != null)
        {
            foreach (Vector3Int pos in addedWalls)
            {
                wallsTilemap.SetTile(pos, null);
            }
        }

        // 3. Spawn Portál
        if (exitPortalPrefab != null && portalSpawnPoint != null)
        {
            Instantiate(exitPortalPrefab, portalSpawnPoint.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}