// using System;
// using System.Collections;
// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;
//
// public class WindShieldToggle : NetworkBehaviour {
//     public bool isOpaque = false;
//    
//     public MeshRenderer WindShieldMeshRenderer;
//    
//     public Material OpaqueMaterial;
//     public Material TransparentMaterial;
//    
//     void Start() {
//         UpdateMaterial(isOpaque);
//     }
//
//     [ContextMenu("Toggle WindShield")]
//     public void ToggleWindShield()
//     {
//         if (!IsServer) return;
//        
//         isOpaque = !isOpaque;
//         UpdateMaterial(isOpaque);
//         UpdateWindshieldClientRpc(isOpaque);
//         
//         Debug.Log("server pressed");
//     }
//    
//     [ClientRpc]
//     private void UpdateWindshieldClientRpc(bool opaque)
//     {
//         if (IsServer) return;
//         
//         Debug.Log("Client received");
//        
//         UpdateMaterial(opaque);
//     }
//    
//     private void UpdateMaterial(bool opaque)
//     {
//         Debug.Log("update mat");
//         WindShieldMeshRenderer.material = opaque ? OpaqueMaterial : TransparentMaterial;
//     }
//
//     private void OnGUI() {
//         if (IsServer) {
//             GUI.skin.button.fontSize = 20;
//             if (GUI.Button(new Rect(200, 30, 150, 70), $"Opaque: {isOpaque}")) {
//                 ToggleWindShield();
//             }
//         }
//     }
// }