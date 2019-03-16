/******************************************************************************
 * Derived by 
 * 
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System;
using UnityEngine.Networking;
using Leap.Unity.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity
{

    /// <summary>
    /// The HandModelManager manages a pool of HandModelBases and makes HandRepresentations
    /// when a it detects a Leap Hand from its configured LeapProvider.
    /// 
    /// When a HandRepresentation is created, a HandModelBase is removed from the pool.
    /// When a HandRepresentation is finished, its HandModelBase is returned to the pool.
    /// 
    /// This class was formerly known as HandPool.
    /// </summary>
    public class NetworkHandModelManager : MonoBehaviour
    {

        public GameObject LeftHandPrefab;
        public GameObject RightHandPrefab;
        class RenderingHandModel
        {
            public HandModelBase HMBL;
            public long FrameIdLeft;
            public float lastUpdateLeft;

            public HandModelBase HMBR;
            public long FrameIdRight;
            public float lastUpdateRight;
        };
        private Dictionary<NetworkInstanceId, RenderingHandModel> PlayerModelInstances = new Dictionary<NetworkInstanceId, RenderingHandModel>();
        Dictionary<NetworkInstanceId, RemoteHandManager.RemoteObjectSync> localCopy = new Dictionary<NetworkInstanceId, RemoteHandManager.RemoteObjectSync>();
        protected bool graphicsEnabled = true;
        RemoteHandManager _remoteHandData;

        private void Start()
        {
            _remoteHandData = GetComponentInParent<RemoteHandManager>();
            Init();
        }

        public void Init()
        {
            PlayerModelInstances.Clear();
            localCopy.Clear();
        }
        private void Update()
        {



            lock (_remoteHandData.PubRemoteSyncedObjectsDict)
            {
                localCopy = new Dictionary<NetworkInstanceId, RemoteHandManager.RemoteObjectSync>(_remoteHandData.PubRemoteSyncedObjectsDict);
            }
            foreach (KeyValuePair<NetworkInstanceId, RemoteHandManager.RemoteObjectSync> inData in localCopy)
            {
                if (!PlayerModelInstances.ContainsKey(inData.Key))
                {
                    RenderingHandModel RHMB = new RenderingHandModel();
                    RHMB.HMBL = Instantiate(LeftHandPrefab).transform.GetComponent<HandModelBase>();
                    RHMB.HMBR = Instantiate(RightHandPrefab).transform.GetComponent<HandModelBase>();
                    RHMB.lastUpdateLeft = 0;
                    RHMB.lastUpdateRight = 0;
                    RHMB.FrameIdLeft = 0;
                    RHMB.FrameIdRight = 0;
                    PlayerModelInstances.Add(inData.Key, RHMB);
                }
                if (!PlayerModelInstances.ContainsKey(inData.Key))
                {
                    Debug.Log("This should not happen. I just added this!");
                    return;
                }
                if (localCopy[inData.Key].LHand.FrameId > PlayerModelInstances[inData.Key].FrameIdLeft)
                {
                    PlayerModelInstances[inData.Key].HMBL.transform.gameObject.SetActive(true);
                    PlayerModelInstances[inData.Key].FrameIdLeft = localCopy[inData.Key].LHand.FrameId;
                    PlayerModelInstances[inData.Key].HMBL.SetLeapHand(inData.Value.LHand);
                    PlayerModelInstances[inData.Key].HMBL.UpdateHand();
                    PlayerModelInstances[inData.Key].lastUpdateLeft = Time.time;
                }
                else if (Time.time - PlayerModelInstances[inData.Key].lastUpdateLeft > 0.5f)
                {
                    PlayerModelInstances[inData.Key].HMBL.transform.gameObject.SetActive(false);
                }

                if (localCopy[inData.Key].RHand.FrameId > PlayerModelInstances[inData.Key].FrameIdRight)
                {
                    PlayerModelInstances[inData.Key].HMBR.transform.gameObject.SetActive(true);
                    PlayerModelInstances[inData.Key].FrameIdRight = localCopy[inData.Key].RHand.FrameId;
                    PlayerModelInstances[inData.Key].HMBR.SetLeapHand(inData.Value.RHand);
                    PlayerModelInstances[inData.Key].HMBR.UpdateHand();
                    PlayerModelInstances[inData.Key].lastUpdateRight = Time.time;
                }
                else if (Time.time - PlayerModelInstances[inData.Key].lastUpdateRight > 0.5f)
                {
                    PlayerModelInstances[inData.Key].HMBR.transform.gameObject.SetActive(false);
                }




            }

        }
    }
}