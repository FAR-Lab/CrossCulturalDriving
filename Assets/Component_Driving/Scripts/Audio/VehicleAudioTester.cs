﻿/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 */

using UnityEngine;

public class VehicleAudioTester : MonoBehaviour {
    private float loadTest;

    private float rpmTest;

    private VehicleAudio vehicle;

    // Use this for initialization
    private void Awake() {
        vehicle = GetComponent<VehicleAudio>();
    }

    private void Update() {
        vehicle.rpm = rpmTest;
        vehicle.load = loadTest;
    }

    private void OnGUI() {
        rpmTest = GUI.HorizontalSlider(new Rect(20, 20, 400, 40), rpmTest, 500, 7000);
        loadTest = GUI.HorizontalSlider(new Rect(20, 80, 400, 40), loadTest, 0, 1);
        GUI.Label(new Rect(440, 20, 100, 100), "RPM: " + rpmTest);
        GUI.Label(new Rect(440, 80, 100, 100), "Load: " + loadTest);
    }
}