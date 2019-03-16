using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GpsController : MonoBehaviour {

    public enum Direction { Straight, Left, Right, Stop, Hurry ,Loading,None };

    public Sprite straightImage, leftImage, rightImage, StopImage, HurryImage, LoadingImage;
    AudioSource GpsAudioPlayer;

    public Sprite HurryImageEnglish, StopImageEnglish, LoadingImageEnglish;
    public Image gpsImagePlane;
     bool AltLanguge = false; // should get this from the scene maager
    public Direction defaultDirection;

    // Use this for initialization
    void Start() {
        gpsImagePlane.sprite = spriteForDirection(defaultDirection);
        AltLanguge = SceneStateManager.Instance.UseHebrewLanguage;
    }

    // Update is called once per frame
    void Update() {
        if (GpsAudioPlayer == null)
        {
            GpsAudioPlayer = GetComponent<AudioSource>();
        }
    }

    Sprite spriteForDirection(Direction d) {
        if (GpsAudioPlayer != null)
        {
            GpsAudioPlayer.Play();
        }
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
