using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class NPCStateMachine : MonoBehaviour
{
    // Start is called before the first frame update
    private NavMeshAgent nav;
    private NPCConstants.NPCState agentState;
    private Animator anim;
    private Vector3 destination;
    
    //03-13
    private Transform _transform;
    private bool crossingStreet = false;
    private MeshCollider crossWalk = null;
    private bool finishedCrossing = false;
    
    //03-25
    private int bornBlockIndex;
    private int destBlockIndex;

    private List<BoxCollider> destinationAreas;
    public float minDistanceDiff = 50f;
    private bool isPaused = false;
    private bool finishedRegularPause = false;

    public bool useTraffic = false;
    private void Awake() {
        // if (transform.GetComponent<Animator>()) {
        //     anim = transform.GetComponent<Animator>();
        // }
        // else {
        //     anim = transform.GetChild(0).GetComponent<Animator>();
        // }
        anim = transform.GetChild(0).GetComponent<Animator>();
        nav = GetComponent<NavMeshAgent>();
        if (!nav) {
            Debug.Log("Non-Player Character Not Found.");
        }

        _transform = GetComponent<Transform>();
        if (!_transform) {
            Debug.Log("Cannot get transform of NPC.");
        }
    }

    void Start() {
        destinationAreas = BlocksManager.GetBuildingBlocks();
        agentState = NPCConstants.NPCState.Idle;
        anim.SetBool("isIdling", true);
    }

    // Update is called once per frame
    void Update()
    {
        switch (agentState) {
            case NPCConstants.NPCState.Idle:
                SetTargetDestination();
                break;
            case NPCConstants.NPCState.Move:
                MoveToTarget();
                break;
            case NPCConstants.NPCState.CrossStreet:
                CrossStreet();
                break;
            case NPCConstants.NPCState.Pause:
                // isPaused = true;
                if (!isPaused || finishedRegularPause) {
                    StartCoroutine(Pause());
                }

                break;
            default:
                break;
        }
    }

    void setUseTraffic(bool useTraffic) {
        this.useTraffic = useTraffic;
    }

    //TODO: Replace anim with new animator along with blend tree animation
    private void FixedUpdate() {
        switch (agentState) {
            case NPCConstants.NPCState.Idle:
                anim.SetBool("isIdling",true);
                
                break;
            case NPCConstants.NPCState.Move:
                anim.SetBool("isIdling", false);
                // anim.SetBool("isChecking",false);
                break;
            case NPCConstants.NPCState.Pause:
    
                anim.SetBool("isIdling",true);
                // anim.SetBool("isChecking",true);
                break;
            case NPCConstants.NPCState.CrossStreet:
                anim.SetBool("isIdling",false);
                // anim.SetBool("isChecking",false);
                break;
        }
    }

    //03-25
    //Get a random dest block index;Get a random location in that block
    //Set as the NPC's targetDestination
    Vector3 SelectRandomDestination() {
        if (destinationAreas != null) {
            int index = Random.Range(0, destinationAreas.Count);
            //TODO:Rethink if needs to make dest and start in different blocks; now only checks the distance between the two position
            
            // //destBlock has to be different from bornBlock
            // while (index == bornBlockIndex) {
            //     index = Random.Range(0, destinationAreas.Count);
            // }

            BoxCollider destBox = destinationAreas[index];
            Vector3 randomDest = new Vector3(
                Random.Range(destBox.bounds.min.x,destBox.bounds.max.x),
                _transform.position.y,
                Random.Range(destBox.bounds.min.z,destBox.bounds.max.z)
            );
            
            NavMeshHit hit;
            int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
            NavMeshPath path = new NavMeshPath();
            while (! NavMesh.SamplePosition(randomDest, out hit, 5f, walkableMask) || Vector3.Distance(
                       hit.position,_transform.position)<=minDistanceDiff ) {
                randomDest = new Vector3(
                    Random.Range(destBox.bounds.min.x,destBox.bounds.max.x),
                    _transform.position.y,
                    Random.Range(destBox.bounds.min.z,destBox.bounds.max.z)
                );
            }

            // if (!nav.CalculatePath(hit.position, path)
            //     || path.status != NavMeshPathStatus.PathComplete) {
            //     CrowdAgentManager.Singleton.DestroyAgent(gameObject.GetComponent<CrowdAgent>());
            // }

            return hit.position;
        }
        return new Vector3(-1, -1, -1);
    }

    //TODO: Move Mode using blend tree
    void SetMoveMode(NPCConstants.NPCMoveMode mode) {
        // agentMoveMode = mode;
        return;
    }

    //set the final destination for the NPC
    //NOTE: THIS IS DIFFERENT FROM NAVMESHAGENT.SETDESTINATION
    void SetTargetDestination() {
        Vector3 dest = SelectRandomDestination();
        if (dest == -1 * Vector3.one) {
            Debug.Log("No destination available, please check the destination areas in game.");
            return;
        }

        destination = dest;
        nav.SetDestination(destination);
        // anim.SetBool("isIdling",false);
        agentState = NPCConstants.NPCState.Move;
        
        // print("Switch to Move");
    }

    void MoveToTarget() {
        //after crossing crosswalk, first check if has destination, if so, continue to original path
        if (finishedCrossing) {
            if (destination != -1 * Vector3.one) {
                print("After sidewalk");
                nav.SetDestination(destination);
                finishedCrossing = false;
                return;
            }
        }
        
        //if reach the destination, reset
        if (nav.isOnNavMesh && !nav.pathPending && nav.remainingDistance < 0.3f) {
            print("Finished Path");
            // anim.SetBool("isIdling",true);
            agentState = NPCConstants.NPCState.Idle;
            
            return;
        }
        
        //while moving to final destination, check if about to cross the street continuously
        crossWalk = (MeshCollider) GetCrosswalk();
        if(crossWalk != null) {
            //Check if the closest point is at corner(don't want the agents to walks diagonally across the street)
            Vector3 closestPoint = crossWalk.ClosestPoint(_transform.position);
            if (closestPoint.x.Equals(crossWalk.bounds.min.x)  && 
                closestPoint.z.Equals(crossWalk.bounds.min.z)) {
                return;
            }
            if (closestPoint.x.Equals(crossWalk.bounds.max.x) &&
                      closestPoint.z.Equals(crossWalk.bounds.max.z)) {
                return;
            }
            if (closestPoint.x.Equals(crossWalk.bounds.max.x) &&
                closestPoint.z.Equals(crossWalk.bounds.min.z)) {
                return;
            }
            if (closestPoint.x.Equals(crossWalk.bounds.min.x) &&
                closestPoint.z.Equals(crossWalk.bounds.max.z)) {
                return;
            }
            
            // anim.SetBool("isIdling",true);
            agentState = NPCConstants.NPCState.Pause;
            
        }
    }

    Collider GetCrosswalk() {
        // need to return the meshcollider in front of the character
        Collider collider = CloseToCrosswalk();
        if (collider != null) {
            try {
                MeshCollider mesh = (MeshCollider)collider;
                return mesh;
            }
            catch {
                return null;
            }
        }
        return null; 
    }

    //03-13 state machine functions
    IEnumerator Pause() {
        if (!isPaused) {
            isPaused = true;
            
            //set random stand pose, only when switch from move to idle
            int standIndex = Random.Range(0, 3);
            anim.SetInteger("StandNum",standIndex);
            // anim.SetBool("isIdling",true);
            
            //Stop and Turn to Face Crosswalk
            nav.isStopped = true;
        
            TurnToFaceCrosswalk();
            
            //TODO: should do look around animation here; also do random wait time
            float waitTime = Random.Range(2, 3);
            yield return new WaitForSeconds(waitTime);
            finishedRegularPause = true;
        }
        
        //
        bool carApproaching = false;
        bool trafficRed = true;
        Vector3[] raycastPoints = new Vector3[5];
        Vector3[] rayDirections = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

        for (int i = 0; i < 5; i++) {
            raycastPoints[i] = new Vector3(_transform.position.x,_transform.position.y,_transform.position.z)+_transform.forward*i;
        }
        //check traffic light
        trafficRed = CheckTrafficLight();
    
        //check car--get NetworkVehicle Component
        // need to cast multiple direction
        foreach (Vector3 ray in raycastPoints) {
            foreach (Vector3 direction in rayDirections) {
                RaycastHit[] hits;
                hits = Physics.RaycastAll(ray,direction,maxDistance:10f);
                if(hits.Length>0){
                    foreach (RaycastHit hit in hits) {
                        if (hit.collider.gameObject.transform.parent.GetComponent<NetworkVehicleController>()) {
                            // also need to check if the car is actually moving at certain speed
                            carApproaching = true;
                            break;
                        }
                    }
                }
            }
        }

        if (!carApproaching && !trafficRed) {
            //switch state to cross street
            yield return new WaitForSeconds(Random.Range(1f,2f));
            isPaused = false;
            // anim.SetBool("isIdling",false);
            agentState = NPCConstants.NPCState.CrossStreet;
            
        }
    }

    //replace this with similar code to vehicle traffic checking(trigger based)
    bool CheckTrafficLight() {
        //always return false if not using traffic system
        if (!useTraffic) {
            return false;
        }

        if (crossWalk != null) {
            DummyTrafficLight trafficLight = crossWalk.GetComponentInParent<DummyTrafficLight>();
            if (trafficLight != null && trafficLight.isRed) {
                return true;
            }
        }

        return false;
    }

    void CrossStreet() {
        //set intermediate destination to some point across the street
        //move to the point across the street
        if (!crossingStreet && crossWalk != null) {
            // MeshCollider collider = (MeshCollider) getCrosswalk();
            float size = Math.Max(crossWalk.bounds.size.x, crossWalk.bounds.size.z)+0.5f; //0.5 is some buffering parameter
            Vector3 crossDestination = _transform.position+_transform.forward.normalized*size;
            nav.isStopped = false;
            nav.SetDestination(crossDestination);
            crossingStreet = true;
        }
        else {
            if (!nav.pathPending && nav.remainingDistance < 0.3f) {
                // print("Finishing Crosswalk");
                //finish crossing, set to regular move to target
                crossingStreet = false;
                finishedCrossing = true;
                crossWalk = null;
                // anim.SetBool("isIdling",false);
                agentState = NPCConstants.NPCState.Move;
                
            }
        }
    }

    void TurnToFaceCrosswalk() {
        print("Turn to face crosswalk");
        RaycastHit[] hits;
        Vector3 unit_forward = _transform.forward;
        unit_forward.Normalize();
        unit_forward *= 0.5f;
        Vector3 direction = new Vector3(unit_forward.x,-1,unit_forward.z);

        hits = Physics.RaycastAll(_transform.position, direction, 2.5f);
        if (hits.Length >0) {
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].collider.gameObject.layer == LayerMask.NameToLayer("crosswalk")) {
                    Vector3 closestPoint = hits[i].collider.gameObject.GetComponent<MeshCollider>().ClosestPoint(_transform.position);
                    _transform.LookAt(new Vector3(closestPoint.x,_transform.position.y,closestPoint.z));
                    return;
                }
            }
        }
    }
    
    Collider CloseToCrosswalk() {
        RaycastHit[] hits;
        Vector3 unit_forward = _transform.forward;
        unit_forward.Normalize();
        unit_forward *= 0.5f;
        Vector3 direction = new Vector3(unit_forward.x,-1,unit_forward.z);
        // Vector3 direction = new Vector3(transform.forward.x,-1,transform.forward.z);
        hits = Physics.RaycastAll(_transform.position, direction, 1.5f);
        if (hits.Length >0) {
            for (int i = 0; i < hits.Length; i++) {
                if (hits[i].collider.gameObject.layer == LayerMask.NameToLayer("crosswalk")) {
                    return hits[i].collider;
                }
            }
        }
        return null;
    }
}
