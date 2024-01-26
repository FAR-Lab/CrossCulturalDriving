using System;
using System.Collections.Generic;
using UnityEngine;


/*
 //https://ttsmaker.com/
Please cross the street.

Please turn left.

Please turn right.

Please stop.

Please hurry up.

Please wait.

At the intersection turn right

At the intersection turn left.

Please come to a  stop.

Please go to the hospital.

> Scene 3: Walk towards the bus stop
> Scene 7: Walk towards the food cart with the umbrella
> Scene 12: Walk to the red building (???)/ black trash can
> Scene 21: Walk towards the left entrance of the red building
> Scene 15: Walk towards the blue trash can(???)/ walk towards the grey building with the columns
> Scene 16: Walk towards the bus stop 
> Scene 106: walk towards the gate (for parking lot)
> Scene 101: Walk towards the black trash can/ stairs across the street
> Scene 102: Walk towards the black trash can/ stairs across the street
> Scene 103: Walk towards the black trash can/ stairs across the street
> Scene 104: Walk towards the park entrance
> Scene 105: Walk towards the hotel entrance
 */
public class PedestrianNavigationAudioCues : MonoBehaviour {

    public float Volume=0.8f;

    private AudioClip straightSound,
        leftSound,
        rightSound,
        StopSound,
        HurrySound,
        LoadingSound,
        StartRightSound,
        StartLeftSound,
        ComeToStopSound,
        ToHospitalSound,
        BusStopSound,
        BlackTrashcanSound,
        BlueTrashcanSound,
        FoodCartSound,
        GateSound,
        GreyBuildingSound,
        GreyBuildingColumnsSound,
        HotelEntranceSound,
        ParkEntranceSound,
        RedBuildingSound,
        StaircaseSound,
        StairsAcrossStreetSound;

    private AudioSource src;

    private bool readyToplay = false;
    // Start is called before the first frame update
    void LoadResources()
    {
        
        straightSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Cross");
        leftSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Left");
        rightSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Right");
        StopSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Stop");
        HurrySound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Hurry");
        LoadingSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Wait");
        StartRightSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/IntersectionRight");
        StartLeftSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/IntersectionLeft");
        ComeToStopSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/ComeToStop");
        ToHospitalSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/Hospital");
        BusStopSound = Resources.Load<AudioClip>(path: "PedestrianNavigationSounds/new_cues/bus_stop");
        BlackTrashcanSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/black_trashcan");
        BlueTrashcanSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/blue_trashcan");
        FoodCartSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/food_cart");
        GateSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/gate");
        GreyBuildingSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/grey_building");
        GreyBuildingColumnsSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/grey_building_columns");
        HotelEntranceSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/hotel_entrance");
        ParkEntranceSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/park_entrance");
        RedBuildingSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/red_building");
        StaircaseSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/staircase");
        StairsAcrossStreetSound=Resources.Load<AudioClip>("PedestrianNavigationSounds/new_cues/stairs_across_street");

        src = gameObject.AddComponent<AudioSource>();
        src.spatialize = true;
        src.spread = 1;
        src.volume = Volume;

        readyToplay = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetNewNavigationInstructions( NavigationScreen.Direction directions ) {
        if (!readyToplay) {
            LoadResources();
        }
        switch (directions) {
            case NavigationScreen.Direction.Straight:
                src.PlayOneShot(straightSound);
                Debug.Log("just played straight sound");
                break;
            case NavigationScreen.Direction.Left:
                src.PlayOneShot(leftSound);
                break;
            case NavigationScreen.Direction.Right:
                src.PlayOneShot(rightSound);
                break;
            case NavigationScreen.Direction.Stop:
                src.PlayOneShot(StopSound);
                break;
            case NavigationScreen.Direction.Hurry:
                src.PlayOneShot(HurrySound);
                break;
            case NavigationScreen.Direction.Loading:
                src.PlayOneShot(LoadingSound);
                break;
            case NavigationScreen.Direction.None:
                // Handle None case, if needed
                break;
            case NavigationScreen.Direction.StartRight:
                src.PlayOneShot(StartRightSound);
                break;
            case NavigationScreen.Direction.StartStraight:
                src.PlayOneShot(StartLeftSound);  
                break;
            case NavigationScreen.Direction.ComeToStop:
                src.PlayOneShot(ComeToStopSound);
                break;
            case NavigationScreen.Direction.ToHospital:
                src.PlayOneShot(ToHospitalSound);
                break;
            case NavigationScreen.Direction.bus_stop:
                src.PlayOneShot(BusStopSound);
                break;
            case NavigationScreen.Direction.black_trashcan:
                src.PlayOneShot(BlackTrashcanSound);
                break;
            case NavigationScreen.Direction.blue_trashcan:
                src.PlayOneShot(BlueTrashcanSound);
                break;
            case NavigationScreen.Direction.food_cart:
                src.PlayOneShot(FoodCartSound);
                break;
            case NavigationScreen.Direction.gate:
                src.PlayOneShot(GateSound);
                break;
            case NavigationScreen.Direction.grey_building:
                src.PlayOneShot(GreyBuildingSound);
                break;
            case NavigationScreen.Direction.grey_building_columns:
                src.PlayOneShot(GreyBuildingColumnsSound);
                break;
            case NavigationScreen.Direction.hotel_entrance:
                src.PlayOneShot(HotelEntranceSound);
                break;
            case NavigationScreen.Direction.park_entrance:
                src.PlayOneShot(ParkEntranceSound);
                break;
            case NavigationScreen.Direction.red_building:
                src.PlayOneShot(RedBuildingSound);
                break;
            case NavigationScreen.Direction.staircase:
                src.PlayOneShot(StaircaseSound);
                break;
            case NavigationScreen.Direction.stairs_across_street:
                src.PlayOneShot(StairsAcrossStreetSound);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
