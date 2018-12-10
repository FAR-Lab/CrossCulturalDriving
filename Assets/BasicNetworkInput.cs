using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace RVP
{
    [RequireComponent(typeof(VehicleParent))]
    [DisallowMultipleComponent]
    [AddComponentMenu("RVP/Input/Basic Network Input", 0)]

    //Class for setting the input with the input manager
    public class BasicNetworkInput : NetworkBehaviour
    {
        VehicleParent vp;
        public string accelAxis;
        public string brakeAxis;
        public string steerAxis;
        public string ebrakeAxis;
        public string boostButton;
        public string upshiftButton;
        public string downshiftButton;
        public string pitchAxis;
        public string yawAxis;
        public string rollAxis;


        //float accel = 0;
       // float breakVal = 0;
        void Start()
        {
            vp = GetComponent<VehicleParent>();
        }

        void Update()
        {
            //Get single-frame input presses
            if (isLocalPlayer)
            {
                if (!string.IsNullOrEmpty(upshiftButton))
                {
                    if (Input.GetButtonDown(upshiftButton))
                    {
                        vp.PressUpshift();
                    }
                }

                if (!string.IsNullOrEmpty(downshiftButton))
                {
                    if (Input.GetButtonDown(downshiftButton))
                    {
                        vp.PressDownshift();
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                //Get constant inputs
                if (!string.IsNullOrEmpty(accelAxis))
                {
                    float accel = waypoint.scale(-1, 1, 0, 1, Input.GetAxis(accelAxis));
                    vp.SetAccel(accel);

                }
                if (!string.IsNullOrEmpty(brakeAxis))
                {
                        float br = waypoint.scale(-1, 1, 0, 1, Input.GetAxis(brakeAxis));

                        vp.SetBrake(br);
                   
                }

                if (!string.IsNullOrEmpty(steerAxis))
                {
                    vp.SetSteer(Input.GetAxis(steerAxis));
                   // Debug.Log("steer: " + Input.GetAxis(steerAxis).ToString());
                }

                if (!string.IsNullOrEmpty(ebrakeAxis))
                {
                    vp.SetEbrake(Input.GetAxis(ebrakeAxis));
                }

                if (!string.IsNullOrEmpty(boostButton))
                {
                    vp.SetBoost(Input.GetButton(boostButton));
                }

                if (!string.IsNullOrEmpty(pitchAxis))
                {
                    vp.SetPitch(Input.GetAxis(pitchAxis));
                }

                if (!string.IsNullOrEmpty(yawAxis))
                {
                    vp.SetYaw(Input.GetAxis(yawAxis));
                }

                if (!string.IsNullOrEmpty(rollAxis))
                {
                    vp.SetRoll(Input.GetAxis(rollAxis));
                }

                if (!string.IsNullOrEmpty(upshiftButton))
                {
                    vp.SetUpshift(Input.GetAxis(upshiftButton));
                }

                if (!string.IsNullOrEmpty(downshiftButton))
                {
                    vp.SetDownshift(Input.GetAxis(downshiftButton));
                }
            }
        }
    }
}