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
 */
public class PedestrianNavigationAudioCues : MonoBehaviour {

    public float Volume=0.8f;
    AudioClip straightSound,
        leftSound,
        rightSound,
        StopSound,
        HurrySound,
        LoadingSound,
        StartRightSound,
        StartLeftSound,
        ComeToStopSound,
        ToHospitalSound;

    private AudioSource src;
    
    // Start is called before the first frame update
    void Start()
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

        src = gameObject.AddComponent<AudioSource>();
        src.spatialize = true;
        src.spread = 1;
        src.volume = Volume;
       

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetNewNavigationInstructions(Dictionary<ParticipantOrder, NavigationScreen.Direction> directions , ParticipantOrder po ) {
        NavigationScreen.Direction tmp = directions[po];
        switch (tmp) {
            case NavigationScreen.Direction.Straight:
                src.PlayOneShot(straightSound);
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

}
