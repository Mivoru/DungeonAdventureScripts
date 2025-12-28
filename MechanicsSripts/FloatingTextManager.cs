using UnityEngine;

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
}