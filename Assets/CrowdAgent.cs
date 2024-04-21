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
    
    //NPC change
    private bool isStopped = false;
    private bool onCrossWalk = false;
    private bool finishedCrosswalk = false;

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

        //NPC change
        // SetRandomDestination();
    }
    
    //NPC change: commented out original codes
    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Space))
        // {
        //     SetRandomDestination();
        // }

        // if (!agent.pathPending && agent.remainingDistance <= 0.3f && !isWaiting)
        // {
        //     StartCoroutine(WaitAndSetRandomDestination());
        // }
        
        // //see if is about to go cross crosswalk
        // if (!isStopped && CloseToCrosswalk()) {
        //     animator.SetBool("isIdling", true);
        //     if (!isStopped) {
        //         isStopped = true; //set to true the first time see a crosswalk
        //         agent.isStopped = true;
        //         TurnToFaceCrosswalk();
        //     }
        // }
        // // print(transform.position);
        // if (!finishedCrosswalk && !agent.pathPending && agent.remainingDistance < 0.3f) {
        //     //move to final destination
        //     print("move to final");
        //     finishedCrosswalk = true;
        //     agent.SetDestination(targetPosition);
        //     
        //     //0306 test multiple crosswalks
        //     isStopped = false;
        // }
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

    void SetRandomDestination(int attempts = 0) {

        if (attempts >= maxAttempts) return;
        
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        
        //if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        //{
        //    Destroy(gameObject, 0.1f);
        //    return;
        //}
        
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            NavMeshPath path = new NavMeshPath();
            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetDestination(hit.position);
                targetPosition = hit.position;
            }
            else
            {
                SetRandomDestination(attempts + 1);
            }
        }
        else
        {
            SetRandomDestination(attempts + 1);
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
