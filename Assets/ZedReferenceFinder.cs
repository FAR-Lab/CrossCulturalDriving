using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZedReferenceFinder : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        ConnectionAndSpawning.Singleton.ServerStateChange += UpdateOnReady;
    }
    private void UpdateOnReady(ActionState state) {
        if (state == ActionState.READY || state == ActionState.WAITINGROOM) {
            var z = FindObjectOfType<ZedSpaceReference>();
            if (z != null) {
                transform.position= z.transform.position;
                transform.rotation= z.transform.rotation;
            }
            else {
                Debug.LogWarning("Could not find ZedSpaceReference, not sure where to go?!");
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnDestroy()
    {
        ConnectionAndSpawning.Singleton.ServerStateChange -= UpdateOnReady;
    }
}
