using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;


public class TrafficLightGreen : MonoBehaviour
{

    public Material off;
    public Material red;
    public Material green;
    public Material yellow;

    public GameObject Object;

    private Renderer ren;
    private Material[] mat; // 0 = yellow, 1 = red, 2 = green

    void Start()
    {
        
    }
    void Update()
    {
        if (Input.GetKeyDown("g"))
        {

            StartCoroutine(GoGreenClientRpc());
        }

    }

    [ClientRpc]
    public void InstantGreenClientRpc()
    {
        ren = Object.GetComponent<Renderer>();
        mat = ren.materials;
        
        mat[0] = off;
        mat[1] = off;
        mat[2] = green;

        ren.materials = mat;
    }
    
    [ClientRpc]
    public IEnumerator GoGreenClientRpc()
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