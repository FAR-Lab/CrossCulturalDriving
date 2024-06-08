using UnityEngine;

//Attach this script to a GameObject to rotate around the target position.
public class Spinner : MonoBehaviour {
    //Assign a GameObject in the Inspector to rotate around
    public GameObject target;

    private void Update() {
        // Spin the object around the target at 20 degrees/second.
        transform.RotateAround(target.transform.position, Vector3.up, 20 * Time.deltaTime);
    }
}