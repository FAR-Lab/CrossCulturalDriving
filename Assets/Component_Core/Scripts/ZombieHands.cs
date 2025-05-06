using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieHands : MonoBehaviour {
    private bool initDone = false;

    private Transform m_followTransform;
   
    public void Init(Transform followTransform) {

        m_followTransform = followTransform;
        initDone = true;
    }

    public void Reset() {
        initDone = false;
        m_followTransform = null;
    }

    private void LateUpdate() {
        if (initDone) {
            if (m_followTransform == null) {
                initDone = false;
                return;
            }
            transform.position = m_followTransform.position;
        }
        
        
    }
}
