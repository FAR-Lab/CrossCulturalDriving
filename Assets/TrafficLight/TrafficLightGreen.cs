using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class TrafficLightGreen : NetworkBehaviour
{

    public Material off;
    public Material red;
    public Material green;
    public Material yellow;

    public GameObject Object;

    private Renderer ren;
    private Material[] mat; // 0 = yellow, 1 = red, 2 = green

    private bool Toggle = true;
    void Start()
    {
        
    }
    void Update()
    {
        /*if (!IsServer)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (Toggle)
            {
                StartGreenCoroutineClientRpc();
                Toggle = false;
            }
            else
            {
                StartRedCoroutineClientRpc();
                Toggle = true;
            }
        }*/
    }

    [ClientRpc]
    public void StartGreenCoroutineClientRpc()
    {
        StartCoroutine(GoGreen());  
    }
    [ClientRpc]
    public void StartRedCoroutineClientRpc()
    {
        StartCoroutine(GoRed());  
    }
    private IEnumerator GoRed()
    {

        ren = Object.GetComponent<Renderer>();
        mat = ren.materials;
    
        mat[0] = yellow;
        mat[1] = off;
        mat[2] = off;

        ren.materials = mat;

        yield return new WaitForSeconds(1);
        
        mat[0] = off;
        mat[1] = red;
        mat[2] = off;

        ren.materials = mat;

    }
    
    private IEnumerator GoGreen()
    {

        ren = Object.GetComponent<Renderer>();
        mat = ren.materials;
    
        mat[0] = yellow;
        mat[1] = red;
        mat[2] = off;

        ren.materials = mat;

        yield return new WaitForSeconds(1);
        
        mat[0] = off;
        mat[1] = off;
        mat[2] = green;

        ren.materials = mat;

    }
}
   
