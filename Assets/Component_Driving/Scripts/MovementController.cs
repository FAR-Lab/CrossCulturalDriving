using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour {

  public float speed;

  private Rigidbody rb;

	// Use this for initialization
	void Start () {
    rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
    float h = Input.GetAxis("Horizontal");
    float v = Input.GetAxis("Vertical");

    Vector3 m = new Vector3(h, 0.0f, v);

    rb.AddForce(m * speed);
	}
}
