using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackMirrorAnchor : MonoBehaviour {

    public Transform CameraPos;
    public Transform LowerTarget;
    Vector3 InitalUpperPosition;
    public float minIn; // min and max y CameraPos local position height
    public float maxIn;

	// Use this for initialization
	void Start () {
        InitalUpperPosition = transform.localPosition;

    }
	
	// Update is called once per frame
	void Update () {
        if (SceneStateManager.Instance.ActionState != ActionState.DRIVE)
        {
            float lerpValue = Mathf.Lerp(0, 1, ((CameraPos.localPosition.y - minIn) / (maxIn - minIn)));
;            transform.localPosition = Vector3.Lerp(LowerTarget.localPosition, InitalUpperPosition, lerpValue);
            Debug.Log(lerpValue);
        }
    }
}
