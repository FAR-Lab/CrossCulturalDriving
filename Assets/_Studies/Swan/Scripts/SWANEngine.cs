using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SWANEngine : MonoBehaviour
{

    public enum AudioType
    {
        Beacon,
        Success
    };


    public AudioClip BeaconClip;
    public AudioClip DemoFeatureClip;
    public AudioClip SuccessClip;
    public AudioClip BushFeatureClip;
    public AudioClip BenchFeatureClip;
    public AudioClip HydrantFeatureClip;
    public AudioClip FountainFeatureClip;

    [SerializeField]
    protected int maxSources = 32;

    protected List<SWANAudioSource> freeSources;

    [SerializeField]
    SWANAudioSource audioSourcePrefab;


    private UnityAction<AudioType, Vector3, float, float> spatialAudioEventListener;


    protected bool isInit = false;

    private static SWANEngine swanEngine;

    public static SWANEngine Instance
    {
        get
        {
            if (!swanEngine)
            {
                swanEngine = FindObjectOfType(typeof(SWANEngine)) as SWANEngine;

                if (!swanEngine)
                {
                    Debug.LogError("There needs to be one active SWANEngine script on a GameObject in your scene.");
                }
                else
                {
                    swanEngine.Init();
                }
            }

            return swanEngine;
        }
    }

    void Init()
    {
        if (isInit)
            return;

        freeSources = new List<SWANAudioSource>(maxSources);

        for (int i = 0; i < maxSources; ++i)
        {
            var s = Instantiate<SWANAudioSource>(audioSourcePrefab);
            freeSources.Add(s);
        }


        spatialAudioEventListener = new UnityAction<AudioType, Vector3, float, float>(SpatialAudioEventHandler);

        isInit = true;
    }


    void OnEnable()
    {

        EventManager.StartListening<SpatialAudioEvent, AudioType, Vector3, float, float>(spatialAudioEventListener);


    }

    void OnDisable()
    {

        EventManager.StopListening<SpatialAudioEvent, AudioType, Vector3, float, float>(spatialAudioEventListener);

    }

    void Awake()
    {
        this.Init();
    }



    void SpatialAudioEventHandler(AudioType at, Vector3 worldPos, float mindist, float maxdist)
    {

        if (freeSources.Count > 0)
        {

            var s = freeSources[freeSources.Count - 1];
            freeSources.RemoveAt(freeSources.Count - 1);

            s.transform.position = worldPos;
            s.AudioSource.minDistance = mindist;
            s.AudioSource.maxDistance = maxdist;

            AudioClip ac = null;

            switch(at)
            {
                case AudioType.Beacon:
                    ac = BeaconClip;
                    break;
                case AudioType.Success:
                    ac = SuccessClip;
                    break;
                default:
                    ac = BeaconClip;
                    break;
            }

            s.Play(ac);
        }

    }


    public static void ReturnAudioSource(SWANAudioSource s)
    {
        Instance.freeSources.Add(s);
    }

    void Update()
    {
        
    }
}
