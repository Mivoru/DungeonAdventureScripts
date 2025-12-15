using UnityEngine;
using UnityEngine.AI;

// Každý nepøítel bude mít tento základ
[RequireComponent(typeof(EnemyStats))]
public class BaseEnemyAI : MonoBehaviour
{
    [Header("Data")]
    public EnemyData data; // Odkaz na Scriptable Object (pøetáhneš sem Data_XXX)

    // Protected = vidí to dìti (Archer, Warrior), ale ne ostatní
    protected NavMeshAgent agent;
    protected Animator anim;
    protected Transform player;
    protected EnemyStats stats;
    protected bool isActionInProgress = false;
    protected Vector3 startPosition;

    // Virtuální metody = dìti je mohou pøepsat (override)
    public virtual void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        stats = GetComponent<EnemyStats>();

        // Naètení rychlosti z dat
        if (agent != null && data != null) agent.speed = data.walkSpeed;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        startPosition = transform.position;

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }
    }

    public virtual void Update()
    {
        if (player == null) return;

        // Spoleèná logika otáèení (pokud neútoèíme)
        if (!isActionInProgress)
        {
            RotateTowards(agent.steeringTarget); // Kouká, kam jde
        }

        // Animace pohybu
        if (anim != null && agent != null)
        {
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }
    }

    protected void RotateTowards(Vector3 target)
    {
        // Zjistíme aktuální velikost (absolutní hodnotu, aby byla vždy kladná)
        float sizeX = Mathf.Abs(transform.localScale.x);
        float sizeY = transform.localScale.y;
        float sizeZ = transform.localScale.z;

        if (target.x < transform.position.x)
        {
            // Doleva (otoèíme X do mínusu)
            transform.localScale = new Vector3(-sizeX, sizeY, sizeZ);
        }
        else
        {
            // Doprava (X je plusové)
            transform.localScale = new Vector3(sizeX, sizeY, sizeZ);
        }
    }

    protected bool HasLineOfSight(LayerMask obstacleLayer)
    {
        if (player == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, player.position - transform.position, data.aggroRange, obstacleLayer);
        return hit.collider == null;
    }
}