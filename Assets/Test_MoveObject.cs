using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test_MoveObject : MonoBehaviour {
    public Vector3 direction;

    public float moveSpeed;

    void Update() {
        transform.position += direction * moveSpeed * Time.deltaTime;
    }
}
