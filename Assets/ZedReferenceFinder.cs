using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ZedReferenceFinder : MonoBehaviour {
    public bool m_continuesCalibration;

    public Transform Head;

    public Transform PlayerCamera; 
    // Start is called before the first frame update
    void Start()
    {
        ConnectionAndSpawning.Singleton.ServerStateChange += UpdateOnReady;
    }
    private void UpdateOnReady(ActionState state) {
        if (state is ActionState.READY or ActionState.WAITINGROOM) {
            var z = FindObjectOfType<ExperimentSpaceReference>();
            if (z != null) {
                var t = z.transform.position;
                t.y = 0;
                transform.position = t;
                transform.rotation= z.transform.rotation;
            }
            else {
                Debug.LogWarning("Could not find ZedSpaceReference, not sure where to go?!");
            }
        }

        if (state is ActionState.QUESTIONS
            or ActionState.POSTQUESTIONS 
            or ActionState.LOADINGSCENARIO 
            or ActionState.LOADINGVISUALS) {
         //   m_continuesCalibration = false;
        }
    }
    
    
    // Update is called once per frame
    void Update()
    {
        if (m_continuesCalibration) {
            if (Head == null) {
                Head = FindObjectOfType<SkeletonNetworkScript>()?.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
                if (Head == null) {
                    return;
                }
            }
            Vector3 offset  = PlayerCamera.position - Head.position;
           // Debug.Log("WTF");
            Debug.DrawRay( PlayerCamera.position ,offset,Color.green);
            float angle = Vector3.SignedAngle(new Vector3(PlayerCamera.forward.x, 0, PlayerCamera.forward.z),
                new Vector3(Head.forward.x, 0, Head.forward.z)
                , Vector3.up);
            transform.position += offset*0.25f ;
//transform.rotation*= Quaternion.Euler(0,angle*0.25f,0);
        }
      
    }
    
    void OnDestroy()
    {
        if (ConnectionAndSpawning.Singleton != null) {
            ConnectionAndSpawning.Singleton.ServerStateChange -= UpdateOnReady;
        }
    }

    public void RunCalibration() {
        UpdateOnReady(ActionState.READY);//dirty TODO encapsulate in another function!
    }

    public void InitiateInverseContinuesCalibration(Transform PlayerCamera) {
            m_continuesCalibration = true;
            this.PlayerCamera = PlayerCamera;
    }
}
