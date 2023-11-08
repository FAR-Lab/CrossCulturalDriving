using UnityEngine;
using UnityEngine.AI;

public class CrowdAgent : MonoBehaviour
{
    private NavMeshAgent agent;
    public float radius = 10f;
    private Vector3 targetPosition;

    public bool showGizmos = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetRandomDestination();
    }

    private void Update()
    {
        // for debugging
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRandomDestination();
        }

        if (!agent.pathPending && agent.remainingDistance <= 0.3f)
        {
            SetRandomDestination();
        }
    }

    void SetRandomDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;

        // check if is a valid position (if it's on the navmesh)
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();

            // attempt to calculate a path to the target position
            // if it's possible, set the destination
            // if it's not possible, set a new random destination
            // probably don't need both conditions, but safest this way
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(hit.position);
                targetPosition = hit.position;
            }
            else
            {
                SetRandomDestination();
            }
        }
        else
        {
            SetRandomDestination();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetPosition, 1f);

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, targetPosition);
    }
}
