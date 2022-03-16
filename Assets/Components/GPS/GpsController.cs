using System.Collections;
using System.Collections.Generic;
using UltimateReplay;
using UnityEngine;
using UnityEngine.UI;

public class GpsController : ReplayBehaviour
{
    public enum Direction : int
    {
        Straight,
        Left,
        Right,
        Stop,
        Hurry,
        Loading,
        None
    };

    [ReplayVar(false)] public int recordingDirection = (int)Direction.None;

    public Sprite straightImage, leftImage, rightImage, StopImage, HurryImage, LoadingImage;
    AudioSource GpsAudioPlayer;

    public Sprite HurryImageEnglish, StopImageEnglish, LoadingImageEnglish;
    public Image gpsImagePlane;
    bool AltLanguge = false; // should get this from the scene maager
    public Direction defaultDirection;
    private Direction previousDirection=Direction.None;

    // Use this for initialization
    void Start()
    {
        gpsImagePlane.sprite = spriteForDirection(defaultDirection);
        // AltLanguge = SceneStateManager.Instance.UseHebrewLanguage;
        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
            GetComponentInChildren<Canvas>().enabled = true;
            GetComponentInChildren<Image>().enabled = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (GpsAudioPlayer == null)
        {
            GpsAudioPlayer = GetComponent<AudioSource>();
        }

        if (ConnectionAndSpawing.Singleton.ServerState == ActionState.RERUN)
        {
            if (previousDirection != (Direction) recordingDirection)
            {
                SetDirection((Direction) recordingDirection);
                previousDirection = (Direction) recordingDirection;
                Debug.Log("We are playing back updated the GPS!");
            }
        }
    }

    Sprite spriteForDirection(Direction d)
    {
        switch (d)
        {
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

    public void SetDirection(Direction newDirection)
    {
        if (previousDirection != newDirection)
        {
            previousDirection = newDirection;
            gpsImagePlane.sprite = spriteForDirection(newDirection);
            if (GpsAudioPlayer != null)
            {
                GpsAudioPlayer.Play();
            }

            if (ConnectionAndSpawing.Singleton.ServerisRunning)
            {
                recordingDirection = (int) newDirection;
            }
        }
    }
}