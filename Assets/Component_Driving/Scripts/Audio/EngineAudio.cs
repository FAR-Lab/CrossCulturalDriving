/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class Sample {
    public AudioClip clip;
    public float minRpm;
    public float maxRpm;
    public float autoPitchRoot;
    public float volume;
}

public class SampleInstance {
    public Sample sample;
    public AudioSource source;
}

public class EngineAudio : MonoBehaviour {
    public AudioMixerGroup group;
    public string volumeParamName;
    public List<Sample> samples;
    public AnimationCurve fadeCurve;

    private List<SampleInstance> sampleInstances;

    // Use this for initialization
    private void Start() {
        sampleInstances = new List<SampleInstance>();
        foreach (var s in samples) {
            var instance = new SampleInstance();
            instance.sample = s;
            instance.source = gameObject.AddComponent<AudioSource>();
            instance.source.spread = 1;
            instance.source.spatialBlend = 1;
            instance.source.outputAudioMixerGroup = group;
            instance.source.volume = 0f;
            instance.source.clip = s.clip;
            instance.source.Play();
            instance.source.loop = true;
            instance.sample.volume = DbToLinear(instance.sample.volume);
            sampleInstances.Add(instance);
        }
    }

    public static float DbToLinear(float db) {
        return Mathf.Pow(10f, db / 20f);
    }

    public void SetRPM(float rpm) {
        if (sampleInstances == null)
            return;

        for (var i = 0; i < sampleInstances.Count; i++) {
            var instance = sampleInstances[i];
            if (rpm < instance.sample.minRpm || rpm > instance.sample.maxRpm) {
                instance.source.volume = 0f;
            }
            else {
                //crossfade
                if (i >= 1 && sampleInstances[i - 1].sample.maxRpm > rpm) {
                    var prevInstance = sampleInstances[i - 1];
                    var max = prevInstance.sample.maxRpm;
                    var min = instance.sample.minRpm;
                    var t = (rpm - min) / (max - min);
                    instance.source.volume = fadeCurve.Evaluate(t) * instance.sample.volume;
                    prevInstance.source.volume = fadeCurve.Evaluate(1 - t) * prevInstance.sample.volume;
                }
                else {
                    instance.source.volume = instance.sample.volume;
                }

                //autopitch
                var pitch = 1 / instance.sample.autoPitchRoot * rpm;
                instance.source.pitch = pitch;
            }
        }
    }
}