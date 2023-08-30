using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadContainer : MonoBehaviour
{
    public Transform head;
    
    private void Update()
    {
        if (head != null)
        {
            transform.position = head.position;
        }
    }
}
