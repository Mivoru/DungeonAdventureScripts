using UnityEngine;
using TMPro;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager instance;
    public GameObject textPrefab; // Zde pøetáhni ten Prefab z kroku A

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void ShowDamage(int amount, Vector3 position, bool isCrit)
    {
        if (textPrefab != null)
        {
            GameObject go = Instantiate(textPrefab, position, Quaternion.identity);
            FloatingText ft = go.GetComponent<FloatingText>();
            if (ft != null) ft.Setup(amount, isCrit);
        }
    }
    // V FloatingTextManager.cs

    public void ShowHeal(int amount, Vector3 position)
    {
        // Vytvoøíme text stejnì jako u damage
        GameObject textObj = Instantiate(textPrefab, position, Quaternion.identity);

        // Získáme komponentu (pøedpokládám, že tam máš nìjaký skript nebo TextMeshPro)
        TMP_Text tmp = textObj.GetComponentInChildren<TMP_Text>();
        if (tmp != null)
        {
            tmp.text = "+" + amount.ToString();
            tmp.color = Color.green; // ZELENÁ BARVA!
        }
    }
}