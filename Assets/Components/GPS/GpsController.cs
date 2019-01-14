using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GpsController : MonoBehaviour {

    public enum Direction { Straight, Left, Right, Stop, Hurry };

    public Sprite straightImage, leftImage, rightImage, StopImage, HurryImage;

    public Image gpsImagePlane;

    public Direction defaultDirection;

    // Use this for initialization
    void Start() {
        gpsImagePlane.sprite = spriteForDirection(defaultDirection);
    }

    // Update is called once per frame
    void Update() {

    }

    Sprite spriteForDirection(Direction d) {
        switch (d) {
            case Direction.Straight:
                return straightImage;
            case Direction.Left:
                return leftImage;
            case Direction.Right:
                return rightImage;
            case Direction.Stop:
                return StopImage;
            case Direction.Hurry:
                return HurryImage;
            default:
                return null;
        }
    }

    public void SetDirection(Direction newDirection) {
        gpsImagePlane.sprite = spriteForDirection(newDirection);
    }
}
