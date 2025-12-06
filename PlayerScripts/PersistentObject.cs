using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    void Awake()
    {
        // Tento pøíkaz zajistí, že objekt pøežije naètení nové scény
        DontDestroyOnLoad(this.gameObject);
    }
}