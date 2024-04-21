using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
public class BlocksManager : MonoBehaviour {
    private static BlocksManager _instance;
    
    private List<BoxCollider> _buildingBlocks;

    public static BlocksManager Instance {
        get {
            if (_instance == null) {
                Debug.Log("No building blocks attached.");
            }

            return _instance;
        }
    }

    public static List<BoxCollider> GetBuildingBlocks() {
        return _instance._buildingBlocks;
    }

    void Awake() {
        _instance = this;
        print("Awake");
        _instance._buildingBlocks = new List<BoxCollider>();
        for (int i = 0; i< _instance.transform.childCount;i++) {
            _instance._buildingBlocks.Add(_instance.transform.GetChild(i).GetComponent<BoxCollider>());
            
        }
    }

}
