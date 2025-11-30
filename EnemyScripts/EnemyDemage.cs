using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    // Zde už nenastavujeme damage natvrdo (public int damage = 10),
    // ale bereme si ho dynamicky ze statistik.

    private EnemyStats myStats;

    void Start()
    {
        // Najdeme skript se statistikami na stejném objektu
        myStats = GetComponent<EnemyStats>();

        if (myStats == null)
        {
            Debug.LogError("CHYBA: Na nepøíteli chybí skript EnemyStats!");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Najdeme statistiky hráèe
            PlayerStats playerStats = collision.gameObject.GetComponent<PlayerStats>();

            if (playerStats != null && myStats != null)
            {
                // ÚTOK: Použijeme 'baseDamage' z našich statistik
                // Tady v budoucnu mùžeme pøidat logiku (baseDamage * level)
                playerStats.TakeDamage(myStats.baseDamage);
            }
        }
    }
}