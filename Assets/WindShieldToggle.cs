using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WindShieldToggle : NetworkBehaviour
{
    public NetworkVariable<bool> IsOpaque = new NetworkVariable<bool>(false);
    public bool InitIsOpaque = false;
    
    public MeshRenderer WindShieldMeshRenderer;
    
    public Material OpaqueMaterial;
    public Material TransparentMaterial;
    
    void Start()
    {
        // register event
        IsOpaque.OnValueChanged += OnIsOpaqueChanged;
        
        // set initial value
        IsOpaque.Value = InitIsOpaque;
        OnIsOpaqueChanged(IsOpaque.Value, IsOpaque.Value);
    }

    [ContextMenu("Toggle WindShield")]
    public void ToggleWindShield()
    {
        IsOpaque.Value = !IsOpaque.Value;
    }
    
    private void OnIsOpaqueChanged(bool previousValue, bool newValue)
    {
        if (newValue)
        {
            WindShieldMeshRenderer.material = OpaqueMaterial;
        }
        else
        {
            WindShieldMeshRenderer.material = TransparentMaterial;
        }
     
    }

    private void OnGUI() {
        if (IsServer) {
            if (GUI.Button(new Rect(200, 30, 150, 40), "Toggle WindShield")) {
                ToggleWindShield();
            }
        }
    }
}
