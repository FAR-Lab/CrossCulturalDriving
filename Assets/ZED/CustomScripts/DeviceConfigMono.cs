using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DeviceConfigMono : MonoBehaviour {
    private ZedSpaceReference.DeviceConfig m_config;
    // Start is called before the first frame update
    void Start()
    {
        
    }
    
    # if UNITY_EDITOR
    private void OnDrawGizmos() {
        return;
        Gizmos.matrix = Matrix4x4.TRS(transform.position,transform.rotation,Vector3.one);
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.DrawFrustum(Vector3.one*0.21f, 158/4, 0.25f, 4, 1.77777f);

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        style.fontSize = 20;
        style.alignment = TextAnchor.MiddleCenter;
        Handles.Label(transform.position + new Vector3(0,1.5f,0), name, style);
    }
    # endif

    public void setConfig(ZedSpaceReference.DeviceConfig i_config, bool applyTransform=true) {
        m_config = i_config;
        if (applyTransform) {
         
           if(m_config.World.Rotation.Count==3) {
               
               
               Debug.Log(
               $"x:{-Mathf.Rad2Deg*(float)m_config.World.Rotation[0]} "
           +$"y:{Mathf.Rad2Deg*(float)m_config.World.Rotation[1]} "
           +$"z:{-Mathf.Rad2Deg*(float)m_config.World.Rotation[2]}");
               
               
               transform.localRotation = Quaternion.Euler(
                   -Mathf.Rad2Deg*(float)m_config.World.Rotation[0], 
                   Mathf.Rad2Deg*(float)m_config.World.Rotation[1],
                   -Mathf.Rad2Deg*(float)m_config.World.Rotation[2]);

           }
           else {
               Debug.LogError($"Could not deserilize ZedCamera Config Rotation Count was:{m_config.World.Rotation.Count}");
           }
           if(m_config.World.Translation.Count()==3) {
               transform.localPosition = new Vector3(
                   (float)m_config.World.Translation[0],
                   -(float)m_config.World.Translation[1],
                   (float)m_config.World.Translation[2]);

           }
         
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
