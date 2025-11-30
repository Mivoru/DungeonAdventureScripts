using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavMesh : MonoBehaviour
{
    [Header("Settings")]
    public float aggroRange = 5f;

    private NavMeshAgent agent;
    private Transform target;
    private EnemyStats stats; // Odkaz na statistiky

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>(); // Naèteme staty

        // Nastavení pro 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // PØEPIS RYCHLOSTI AGENTA DLE STATISTIK
        if (stats != null)
        {
            agent.speed = stats.movementSpeed;
        }
        else
        {
            Debug.LogWarning($"Enemy {name} nemá komponentu EnemyStats! Používám defaultní rychlost agenta.");
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;

        StartCoroutine(ActivateAgent());
    }

    IEnumerator ActivateAgent()
    {
        yield return new WaitForSeconds(0.35f);

        Vector3 currentPos = transform.position;
        currentPos.z = 0f;
        transform.position = currentPos;

        NavMeshHit hit;
        float searchRadius = 3.0f;

        if (NavMesh.SamplePosition(currentPos, out hit, searchRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            agent.enabled = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (!agent.enabled || !agent.isOnNavMesh || target == null) return;

        // Prùbìžná aktualizace rychlosti (kdyby se zmìnila, napø. zpomalovací kouzlo)
        if (stats != null)
        {
            agent.speed = stats.movementSpeed;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance < aggroRange)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            if (!agent.isStopped) agent.ResetPath();
        }

        // Rotace grafiky smìrem k pohybu
        if (agent.velocity.magnitude > 0.1f)
        {
            Vector2 dir = agent.velocity.normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90);
        }
    }
}