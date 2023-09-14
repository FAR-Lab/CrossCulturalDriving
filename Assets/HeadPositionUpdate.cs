using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HeadPositionUpdate : NetworkBehaviour
{
    public Transform head;
    
    private void Update()
    {
        
            transform.position = head.position; 
            
    }
}
