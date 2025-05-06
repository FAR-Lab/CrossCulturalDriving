using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SWANAudioSource : MonoBehaviour
{

    protected AudioSource _audioSource;

    public bool IsPlaying {
        get {

            return _audioSource.isPlaying;
        }
    }

    public void Play(AudioClip ac)
    {
        this.gameObject.SetActive(true);
        _audioSource.clip = ac;
        _audioSource.Play();
    }

    public AudioSource AudioSource
    {
        get { return _audioSource; }
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();

        if (_audioSource == null)
            Debug.LogError("no audio source");

    }

    // Start is called before the first frame update
    void Start()
    {
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
       if(!IsPlaying)
        {       
            SWANEngine.ReturnAudioSource(this);
            this.gameObject.SetActive(false);
        } 
    }
}
