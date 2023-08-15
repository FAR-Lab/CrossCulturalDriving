using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class selectionBarAnimation : MonoBehaviour {
    float fillAmount;
    Image icon;
    RectTransform m_RectTransform;
    public bool followArtificialBorder;
    public float artificalBorder = 100;

    // Use this for initialization
    void Start () {
        followArtificialBorder = false;
        fillAmount = 0;

        m_RectTransform = GetComponent<RectTransform>();

        icon = GetComponent<Image>(); // the main image with the circle loading bar;
        icon.fillAmount = 1f;
    }
	
	// Update is called once per frame
	void Update () {
        if (icon.fillAmount != fillAmount) {
            icon.fillAmount = fillAmount;
        }
        if (transform.GetSiblingIndex() < transform.parent.childCount) {
            transform.SetSiblingIndex(transform.parent.childCount);
        }

    }
    public void updatePosition(Vector2 vec) {
        // Debug.Log(vec);
        if (followArtificialBorder)
        {
            if (vec.x < artificalBorder)
            {
                vec.x = artificalBorder;
            }
        }
        else
        {
            vec.x += 5;
        }


        m_RectTransform.anchoredPosition = vec;

    }
    public void setPercentageSelection(float tagrget)
    {
        icon.fillAmount = fillAmount = tagrget;
        //ToDo animate a circle to go arround 
    }
}
