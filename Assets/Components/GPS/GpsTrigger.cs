using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GpsTrigger : MonoBehaviour {

  public GpsController.Direction setDirection;

  private void OnTriggerEnter(Collider other) {
    GpsController c = other.GetComponentInChildren<GpsController>();
    if (c != null) {
      c.SetDirection(setDirection);
    }
  }
}
