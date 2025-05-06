using System.Collections;
using System.IO;
using System.Collections.Generic;
using UnityEngine;


public class scr_Selector_Female : MonoBehaviour {

    //public Renderer[] suits;
    
    private int pick = 0;
    private int count = 0;
    private int type = 0;
    public List<GameObject> SuitTrousers;
    public List<GameObject> SkinTrousers;
    public List<GameObject> SuitSkirts;
    public List<GameObject> SkinSkirts;
    private Renderer oRenderer;
    
    


    // Use this for initialization
    void Start()
    {

        // populate list based on tags
        foreach (Transform child in transform)
        {


            if (child.tag == "Female_SkinForTrousers")
                {
                    SkinTrousers.Add(child.gameObject);
                    //Debug.Log(child + " added");
                }

            if (child.tag == "Female_SkinForSkirt")
                {
                    SkinSkirts.Add(child.gameObject);
                    //Debug.Log(child + " added");
                }
            if (child.tag == "Female_TrouserSuit")
            {
                SuitTrousers.Add(child.gameObject);
                //Debug.Log(child + " added");
            }

            if (child.tag == "Female_SkirtSuit")
            {
                SuitSkirts.Add(child.gameObject);
                //Debug.Log(child + " added");
            }


        }
        // Decide which type
        pickType();
        //pick a suit
        pickSuit();
        // pick headType A/B
        pickSkin();
   
    }

    void pickType()
    {
        type = Random.Range(0, 2); // suit or skirt
        //Debug.Log(type);
    }


    // Function for picking suits
    void pickSuit()
    {
        if (type == 0) // 0 is trouser version
        {
            // clear others

            foreach (GameObject o in SuitSkirts)
            {
                oRenderer = o.GetComponentInChildren<Renderer>();
                oRenderer.enabled = false;
            }
            //


            pick = Random.Range(0, SuitTrousers.Count);
            count = 0;

            foreach (GameObject o in SuitTrousers)
            {

                if (count == pick)
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = true;
                }
                else
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = false;
                }
                count++;
            }
        }

        if (type == 1) // 1 is skirt version
        {

            // clear others


            foreach (GameObject o in SuitTrousers)
            {
                oRenderer = o.GetComponentInChildren<Renderer>();
                oRenderer.enabled = false;
            }
                        //

            pick = Random.Range(0, SuitSkirts.Count);
            count = 0;

            foreach (GameObject o in SuitSkirts)
            {

                if (count == pick)
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = true;
                }
                else
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = false;
                }
                count++;
            }
        }


    }
   
   
    

    // Function for picking skins
    void pickSkin()
    {

        if (type == 0) // 0 is trouser version
        {


            foreach (GameObject o in SkinSkirts)
            {
                oRenderer = o.GetComponentInChildren<Renderer>();
                oRenderer.enabled = false;
            }

            pick = Random.Range(0, SkinTrousers.Count);
            
            count = 0;

            foreach (GameObject o in SkinTrousers)
            {

                if (count == pick)
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = true;
                }
                else
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = false;
                }
                count++;
            }

        }

        if (type == 1) // 1 is skirt version
        {

            foreach (GameObject o in SkinTrousers)
            {
                oRenderer = o.GetComponentInChildren<Renderer>();
                oRenderer.enabled = false;
            }

            // now pick a head // the choice here is important to remeber so that we can choose hair styles that suit.
            pick = Random.Range(0, SkinSkirts.Count);

            count = 0;

            foreach (GameObject o in SkinSkirts)
            {

                if (count == pick)
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = true;
                }
                else
                {
                    oRenderer = o.GetComponentInChildren<Renderer>();
                    oRenderer.enabled = false;
                }
                count++;
            }

        }
    }



    // Update is called once per frame
    void Update () {

        if (Input.GetKeyDown("space"))
        {


            pickType();
            // pick a suit
            pickSuit();
            // pick headType A/B
            pickSkin();
       

        }
    }


}

