using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CrowdAgent : NetworkBehaviour
{
    private NavMeshAgent agent;
    public float radius = 10f;
    private Vector3 targetPosition;
    private int maxAttempts = 10;
    public bool showGizmos = false;
    private bool isWaiting = false;
    public float waitTime = 1f;

    private Animator animator;


    void Start()
    {

        animator = transform.GetChild(0).GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer)
        {
            Destroy(agent);
            this.enabled = false;
            return;
        }

        SetRandomDestination();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRandomDestination();
        }

        if (!agent.pathPending && agent.remainingDistance <= 0.3f && !isWaiting)
        {
            StartCoroutine(WaitAndSetRandomDestination());
        }
    }

    IEnumerator WaitAndSetRandomDestination()
    {
        isWaiting = true;
        animator.SetBool("isIdling", true);
        yield return new WaitForSeconds(waitTime);
        SetRandomDestination();
        isWaiting = false;
        animator.SetBool("isIdling", false);
    }

    void SetRandomDestination(int attempts = 0)
    {

        if (attempts >= maxAttempts) return;

        Vector3 vec1 = new Vector3(11, 0f, 92f);
        agent.SetDestination(vec1);
        print("dest is "+agent.destination);
        targetPosition = vec1;

        // Vector3 randomDirection = Random.insideUnitSphere * radius;
        // randomDirection += transform.position;
        // NavMeshHit hit;

        // //if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        // //{
        // //    Destroy(gameObject, 0.1f);
        // //    return;
        // //}

        // if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        // {
        //     NavMeshPath path = new NavMeshPath();
        //     if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
        //     {
        //         print("Setting destination");
        //         agent.SetDestination(hit.position);
        //         targetPosition = hit.position;
        //     }
        //     else
        //     {
        //         SetRandomDestination(attempts + 1);
        //     }
        // }
        // else
        // {
        //     SetRandomDestination(attempts + 1);
        // }
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
