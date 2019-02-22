using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GpsController : MonoBehaviour {

    public enum Direction { Straight, Left, Right, Stop, Hurry ,None };

    public Sprite straightImage, leftImage, rightImage, StopImage, HurryImage;

    public Sprite HurryImageEnglish, StopImageEnglish;
    public Image gpsImagePlane;
    public bool AltLanguge = false;
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
                if (AltLanguge)
                {
                    return StopImage;
                }
                else
                {
                    return StopImageEnglish;
                }
            case Direction.Hurry:
                if (AltLanguge)
                {
                    return HurryImage;
                }
                else
                {
                    return HurryImageEnglish;
                }
            default:
                return null;
        }
    }

    public void SetDirection(Direction newDirection) {
        if (newDirection != Direction.None)
        {
            gpsImagePlane.sprite = spriteForDirection(newDirection);
        }
    }
}
