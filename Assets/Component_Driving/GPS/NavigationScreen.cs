using UltimateReplay;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NavigationScreen : ReplayBehaviour {
    public enum Direction {
        Straight,
        Left,
        Right,
        Stop,
        Hurry,
        Loading,
        None,
        StartRight,
        StartStraight,
        ComeToStop,
        ToHospital,
        bus_stop,
        black_trashcan,
        blue_trashcan,
        food_cart,
        gate,
        grey_building,
        grey_building_columns,
        hotel_entrance,
        park_entrance,
        red_building,
        staircase,
        stairs_across_street
    }

    [ReplayVar(false)] public int recordingDirection = (int)Direction.None;


    public Sprite straightImage,
        leftImage,
        rightImage,
        StopImage,
        HurryImage,
        LoadingImage,
        StartRightImage,
        StartSraightImage,
        ComeToStopImage,
        ToHospital;

    public Sprite HurryImageEnglish, StopImageEnglish, LoadingImageEnglish;
    public Image gpsImagePlane;
    public Direction defaultDirection;
    private readonly bool AltLanguge = false; // should get this from the scene maager
    private AudioSource GpsAudioPlayer;
    private Direction previousDirection = Direction.None;

    // Use this for initialization
    private void Start() {
        gpsImagePlane.sprite = spriteForDirection(defaultDirection);
        // AltLanguge = SceneStateManager.Instance.UseHebrewLanguage;
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) {
            GetComponentInChildren<Canvas>().enabled = true;
            GetComponentInChildren<Image>().enabled = true;
        }
    }

    // Update is called once per frame
    private void Update() {
        if (GpsAudioPlayer == null) GpsAudioPlayer = GetComponent<AudioSource>();

        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN)
            if (previousDirection != (Direction)recordingDirection) {
                SetDirection((Direction)recordingDirection);
                previousDirection = (Direction)recordingDirection;
                Debug.Log("We are playing back updated the GPS!");
            }
    }

    private Sprite spriteForDirection(Direction d) {
        switch (d) {
            case Direction.Straight:
                return straightImage;
            case Direction.Left:
                return leftImage;
            case Direction.Right:
                return rightImage;
            case Direction.Stop:
                if (AltLanguge)
                    return StopImage;
                return StopImageEnglish;
            case Direction.Hurry:
                if (AltLanguge)
                    return HurryImage;
                return HurryImageEnglish;
            case Direction.StartStraight:
                return StartSraightImage;
            case Direction.StartRight:
                return StartRightImage;
            case Direction.ComeToStop:
                return ComeToStopImage;
            case Direction.ToHospital:
                return ToHospital;
            case Direction.Loading:
            case Direction.None:
            case Direction.bus_stop:
            case Direction.black_trashcan:
            case Direction.blue_trashcan:
            case Direction.food_cart:
            case Direction.gate:
            case Direction.grey_building:
            case Direction.grey_building_columns:
            case Direction.hotel_entrance:
            case Direction.park_entrance:
            case Direction.red_building:
            case Direction.staircase:
            case Direction.stairs_across_street:
            default:
                return null;
        }
    }

    public void SetDirection(Direction newDirection) {
        if (newDirection == Direction.None) return;
        if (previousDirection != newDirection) {
            previousDirection = newDirection;
            gpsImagePlane.sprite = spriteForDirection(newDirection);
            if (GpsAudioPlayer != null) GpsAudioPlayer.Play();

            if (ConnectionAndSpawning.Singleton.ServerisRunning) recordingDirection = (int)newDirection;
        }
    }
}