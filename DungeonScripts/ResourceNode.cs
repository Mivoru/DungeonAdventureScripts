using UnityEngine;

public class ResourceNode : MonoBehaviour
{
    [Header("Mining Info")]
    public int maxHealth = 3; // Poèet kopnutí
    public ItemData itemToDrop; // Co z toho padne
    public int dropAmount = 1;
    public GameObject dropPrefab; // LootPickup prefab (pytel/ikonka na zemi)

    [Header("Visuals")]
    public GameObject hitEffect; // Volitelné: Èástice pøi kopnutí

    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeHit(int damage)
    {
        currentHealth -= damage;

        // Zde mùžeš pøidat zvuk cinknutí nebo efekt
        if (hitEffect) Instantiate(hitEffect, transform.position, Quaternion.identity);
        Debug.Log("Cink! Tìžím rudu...");

        if (currentHealth <= 0)
        {
            BreakNode();
        }
    }

    void BreakNode()
    {
        if (dropPrefab != null && itemToDrop != null)
        {
            GameObject loot = Instantiate(dropPrefab, transform.position, Quaternion.identity);

            LootPickup pickup = loot.GetComponent<LootPickup>();
            if (pickup != null)
            {
                pickup.SetItem(itemToDrop, dropAmount);
            }
        }

        Destroy(gameObject);
    }
}