using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetHook : MonoBehaviour {


    public Texture EmptyStreetTexture;
    
    // Start is called before the first frame update
    void Start()
    {
        if (ConnectionAndSpawning.Singleton != null) {
            var t= ConnectionAndSpawning.Singleton.GetScenarioManager().GetFloorTypeForThisScenarion();
            if (t == ScenarioManager.FloorType.EMPTY) {
                GetComponent<MeshRenderer>().material.mainTexture = EmptyStreetTexture;
            }
        }
    }
    
 
}
