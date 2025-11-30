using UnityEngine;
using TMPro;
using System.Collections;

public class LootLogManager : MonoBehaviour
{
    public static LootLogManager instance; // Singleton, aby byl pøístupný odkudkoliv

    [Header("UI References")]
    public GameObject logTextPrefab; // Tvùj prefab textu
    public Transform container;      // Kontejner vlevo dole

    void Awake()
    {
        instance = this;
    }

    public void AddLog(string itemName, Sprite icon = null)
    {
        // Vytvoøíme nový text v kontejneru
        GameObject newLog = Instantiate(logTextPrefab, container);

        // Nastavíme text
        TMP_Text textComp = newLog.GetComponent<TMP_Text>();
        if (textComp != null)
        {
            textComp.text = $"Sebráno: <color=yellow>{itemName}</color>";
        }

        // Automatické znièení po 3 vteøinách
        Destroy(newLog, 3f);

        // (Volitelné) Pokud bys chtìl fade-out efekt, musel bys na prefab dát další skript,
        // ale Destroy pro zaèátek staèí.
    }
}