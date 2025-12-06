using UnityEngine;

public class Singleton : MonoBehaviour
{
    // Statická mapa pro sledování existujících singletonù podle názvu
    private static System.Collections.Generic.Dictionary<string, bool> exists =
        new System.Collections.Generic.Dictionary<string, bool>();

    void Awake()
    {
        if (exists.ContainsKey(name))
        {
            Destroy(gameObject); // Už existuje, znièit duplikát
        }
        else
        {
            exists[name] = true;
            DontDestroyOnLoad(gameObject);
        }
    }
}