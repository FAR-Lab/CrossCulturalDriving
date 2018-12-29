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
using Leap.Unity.Attributes;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

    /// <summary>
    /// The HandModelManager manages a pool of HandModelBases and makes HandRepresentations
    /// when a it detects a Leap Hand from its configured LeapProvider.
    /// 
    /// When a HandRepresentation is created, a HandModelBase is removed from the pool.
    /// When a HandRepresentation is finished, its HandModelBase is returned to the pool.
    /// 
    /// This class was formerly known as HandPool.
    /// </summary>
    public class NetworkHandModelManager : MonoBehaviour {

        

        public GameObject LeftHandPrefab;
        public GameObject RightHandPrefab;

        protected Dictionary<int, HandModelBase> instansiatedModels = new Dictionary<int, HandModelBase>();
        protected bool graphicsEnabled = true;
        RemoteHandManager _remoteHandData;

        private void Start()
        {
            _remoteHandData = GetComponentInParent<RemoteHandManager>();
        }
        private void Update()
        {
            foreach (int key in _remoteHandData.networkHands.Keys)
            {
                if (!instansiatedModels.ContainsKey(key))
                {
                    HandModelBase hmb;
                    if (_remoteHandData.networkHands[key].IsLeft) {
                        Transform temp = Instantiate(LeftHandPrefab).transform;
                        temp.parent = this.transform;
                        hmb = temp.GetComponent<HandModelBase>();
                    }
                    else {
                        Transform temp = Instantiate(RightHandPrefab).transform;
                        temp.parent = this.transform;
                        hmb = temp.GetComponent<HandModelBase>();
                    }
                    hmb.transform.gameObject.SetActive(true);
                    instansiatedModels.Add(key, hmb);
                    
                }
                instansiatedModels[key].SetLeapHand(_remoteHandData.networkHands[key]);

            }
            foreach(int key in instansiatedModels.Keys)
            {
                instansiatedModels[key].UpdateHand();

            }

        }

    }
}