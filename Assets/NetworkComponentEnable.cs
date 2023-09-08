using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class NetworkComponentEnable : NetworkBehaviour
{
    private NetworkVariable<bool> ComponenetEnabled = new NetworkVariable<bool>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public SkinnedMeshRenderer component;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsLocalPlayer)
        {
            ComponenetEnabled.OnValueChanged += OnStateChanged;
        }

    }
    
    public override void OnNetworkDespawn()
    {
        if (!IsLocalPlayer)
        {
            ComponenetEnabled.OnValueChanged -= OnStateChanged;
        }
    }
    
    public void OnStateChanged(bool previous, bool current)
    {
        
        if (component != null){
            component.enabled = current;
        }
    }
 

    void Update()
    {
        if (IsLocalPlayer)
        {
            ComponenetEnabled.Value = component.enabled;
        } 
    }
}
