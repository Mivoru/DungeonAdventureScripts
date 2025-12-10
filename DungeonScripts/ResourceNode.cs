using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Mining Info")]
    public int maxHealth = 3;
    public ItemData itemToDrop;     // CO padne (Musí být pøiøazeno v Inspectoru!)
    public int dropAmount = 1;      // KOLIK toho padne
    public GameObject dropPrefab;   // Pytlík (LootDrop prefab) - Musí být pøiøazeno!

    [Header("Visuals")]
    public GameObject hitEffect;    // Particle efekt (volitelné)

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeHit(int damage)
    {
        currentHealth -= damage;

        // Vizuální efekt (zatøesení)
        StartCoroutine(ShakeEffect());

        if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);

        Debug.Log($"Kop! Zbývá HP: {currentHealth}");

        if (currentHealth <= 0)
        {
            BreakNode();
        }
    }

    void BreakNode()
    {
        // Kontrola chyb
        if (dropPrefab == null || itemToDrop == null)
        {
            Debug.LogError($"CHYBA: Ruda '{name}' nemá nastavený Drop Prefab nebo Item Data!");
            Destroy(gameObject);
            return;
        }

        // --- SPAWN LOOTU (CYKLUS) ---
        // Místo jednoho balíku jich vyhodíme tolik, kolik je dropAmount
        // (Pokud bys mìl dropAmount 100, radìji to omez, ale pro rudy (1-5) je to super)

        for (int i = 0; i < dropAmount; i++)
        {
            // Spawneme ho pøesnì na pozici kamene (nebo s malinkým posunem)
            // O ten hlavní "rozptyl" (výskok) se postará skript LootPickup sám ve svém Startu
            GameObject loot = Instantiate(dropPrefab, transform.position, Quaternion.identity);

            LootPickup pickup = loot.GetComponent<LootPickup>();
            if (pickup != null)
            {
                // Každý kousek pøedstavuje 1 surovinu
                pickup.SetItem(itemToDrop, 1);
            }
        }

        Destroy(gameObject);
    }

    System.Collections.IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.position;
        float time = 0;
        while (time < 0.1f)
        {
            transform.position = originalPos + (Vector3)UnityEngine.Random.insideUnitCircle * 0.05f;
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }
}