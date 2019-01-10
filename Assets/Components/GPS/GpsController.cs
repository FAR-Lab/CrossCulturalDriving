using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GpsController : MonoBehaviour {

  public enum Direction { Straight, Left, Right};

  public Sprite straightImage, leftImage, rightImage;

  public Image gpsImagePlane;

  public Direction defaultDirection;

  // Use this for initialization
  void Start () {
    gpsImagePlane.sprite = spriteForDirection(defaultDirection);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

  Sprite spriteForDirection(Direction d) {
    switch(d) {
      case Direction.Straight:
        return straightImage;
      case Direction.Left:
        return leftImage;
      case Direction.Right:
        return rightImage;
      default:
        return null;
    }
  }

  public void SetDirection(Direction newDirection) {
    gpsImagePlane.sprite = spriteForDirection(newDirection);
  }
}
