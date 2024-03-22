using UnityEngine;

public class BackMirrorAnchor : MonoBehaviour {
    public Transform CameraPos;
    public Transform LowerTarget;
    public float minIn; // min and max y CameraPos local position height
    public float maxIn;
    private Vector3 InitalUpperPosition;

    // Use this for initialization
    private void Start() {
        InitalUpperPosition = transform.localPosition;
    }

    // Update is called once per frame
    private void Update() {
        if (true) //SceneStateManager.Instance.ActionState != ActionState.DRIVE
        {
            var lerpValue = Mathf.Lerp(0, 1, (CameraPos.localPosition.y - minIn) / (maxIn - minIn));
            ;
            transform.localPosition = Vector3.Lerp(LowerTarget.localPosition, InitalUpperPosition, lerpValue);
        }
    }
}