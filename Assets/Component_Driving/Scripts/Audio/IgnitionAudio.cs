﻿/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class IgnitionAudio : MonoBehaviour {
    public AudioClip engineClip;
    public GameObject enginePosition;
    public AudioClip exhaustClip;
    public GameObject exhaustPosition;

    public AudioMixerGroup mixerGroup;

    public float fadeOutDelay;
    public float fadeOutTime;

    private AudioSource engine;
    private AudioSource exhaust;

    private void Awake() {
        engine = enginePosition.AddComponent<AudioSource>();
        exhaust = exhaustPosition.AddComponent<AudioSource>();

        engine.outputAudioMixerGroup = mixerGroup;
        engine.clip = engineClip;
        engine.loop = false;
        engine.playOnAwake = false;
        engine.spread = 1f;

        exhaust.outputAudioMixerGroup = mixerGroup;
        exhaust.clip = exhaustClip;
        exhaust.loop = false;
        exhaust.playOnAwake = false;
        exhaust.spread = 1f;
    }

    public void Play(Action OnComplete) {
        engine.volume = 1;
        exhaust.volume = 1;
        engine.Stop();
        exhaust.Stop();
        engine.Play();
        exhaust.Play();
        StartCoroutine(_FadeOut(OnComplete));
    }

    private IEnumerator _FadeOut(Action OnComplete) {
        yield return new WaitForSeconds(fadeOutDelay);

        if (OnComplete != null)
            OnComplete();
    }
}