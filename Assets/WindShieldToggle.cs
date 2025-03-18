using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WindShieldToggle : NetworkBehaviour
{
    public NetworkVariable<bool> IsOpaque = new NetworkVariable<bool>(false);
    
    public MeshRenderer WindShieldMeshRenderer;
    
    public Material OpaqueMaterial;
    public Material TransparentMaterial;
    
    void Start()
    {
        // register event
        IsOpaque.OnValueChanged += OnIsOpaqueChanged;
        
        // set initial value
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

        string appearanceString = newValue ? "Opaque" : "Transparent";

        NetworkQNManager networkQNManager = FindObjectOfType<NetworkQNManager>();
        networkQNManager.SetParameters(appearance:appearanceString);
    }

    private void OnGUI() {
        if (IsServer) {
            GUI.skin.button.fontSize = 20;
            if (GUI.Button(new Rect(200, 30, 150, 70), $"Opaque: {IsOpaque.Value}")) {
                ToggleWindShield();
            }

        }
    }
}
