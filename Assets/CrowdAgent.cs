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
    
    //0305
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

        // SetRandomDestination();
    }

    //03-13 need to integrate state machine
    //switch agent state
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetRandomDestination();
        }

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
    
    //0305
    bool CloseToCrosswalk() {
        // print("check");
        RaycastHit[] hits;
        Vector3 unit_forward = transform.forward;
        unit_forward.Normalize();
        unit_forward *= 0.5f;
        Vector3 direction = new Vector3(unit_forward.x,-1,unit_forward.z);
        // Vector3 direction = new Vector3(transform.forward.x,-1,transform.forward.z);
        hits = Physics.RaycastAll(transform.position, direction, 2f);
        if (hits.Length >0) {
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].collider.gameObject.layer == 11) {
                    print("forward" +transform.forward+"collider bounds "+hits[i].collider.bounds);
                    print("close to crosswalk"+hits[i].point);
                    //0306 reset finishedCrosswalk to false since about to go cross another one
                    finishedCrosswalk = false;
                    return true;
                }
            }
        }
        // if (Physics.Raycast(GetComponent<Transform>().position, direction, out hit, 10f)) {
        //     print("close to crosswalk");
        // }
        return false;
    }
    
    // need to add traffic and vehicle check
    void TurnToFaceCrosswalk() {
        RaycastHit[] hits;
        Vector3 unit_forward = transform.forward;
        print("forward"+unit_forward);
        unit_forward.Normalize();
        unit_forward *= 0.5f;
        Vector3 direction = new Vector3(unit_forward.x,-1,unit_forward.z);

        // Vector3 direction = new Vector3(transform.forward.x,-1,transform.forward.z);
        hits = Physics.RaycastAll(transform.position, direction, 2.5f);
        if (hits.Length >0) {
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].collider.gameObject.layer == 11) {
                    Vector3 closestPoint = hits[i].collider.gameObject.GetComponent<MeshCollider>().ClosestPoint(transform.position);
                    // closestPoint = new Vector3(-3.5f, 1, 55);
                    print("self: "+transform.position+" hit "+closestPoint);
                    transform.LookAt(new Vector3(closestPoint.x,transform.position.y,closestPoint.z));
                    agent.isStopped = false;
                    // need to make the length relative to the size of the crosswalk
                    // or keep moving forward while on crosswalk
                    animator.SetBool("isIdling", false);
                    print("cross target"+(transform.position+5.1f*transform.forward));
                    agent.SetDestination(transform.position+5.1f*transform.forward);
                    return;
                }
            }
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

    void SetRandomDestination(int attempts = 0) {
        Vector3 dest = new Vector3(-12.5f, 0, 60.79f);
        agent.SetDestination(dest);
        targetPosition = dest;
        // if (attempts >= maxAttempts) return;
        //
        // Vector3 randomDirection = Random.insideUnitSphere * radius;
        // randomDirection += transform.position;
        // NavMeshHit hit;
        //
        // //if (!NavMesh.SamplePosition(transform.position, out hit, 1.0f, NavMesh.AllAreas))
        // //{
        // //    Destroy(gameObject, 0.1f);
        // //    return;
        // //}
        //
        // if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        // {
        //     NavMeshPath path = new NavMeshPath();
        //     if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
        //     {
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
