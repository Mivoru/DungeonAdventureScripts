using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Header("Settings")]
    public string sceneToLoad; // Jméno scény, kam chceme jít (napø. "DungeonScene")
    public Vector3 spawnPosition; // Kde se hráè objeví v nové scénì (volitelné)

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Tady mùžeme v budoucnu uložit hru
            // SaveGame(); 
            AudioManager.instance.PlaySFX("Portal");
            Debug.Log($"Portál aktivován! Cestuji do: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}