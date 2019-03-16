using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR;
using UnityEngine.Networking;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Serialization;
using System;
using Leap.Unity.Attributes;
using Leap.Unity.Encoding;
using Leap.Unity;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkMessageType
{
    public static short uploadHand = MsgType.Highest + 1;
    public static short DownloadHand = MsgType.Highest + 2;
    public static short StateUpdate = MsgType.Highest + 3;
    public static short uploadVRHead = MsgType.Highest + 4;
    public static short DownloadVRHead = MsgType.Highest + 5;
};

public class RemoteHandManager : MonoBehaviour
{

    public Transform DebugHand;
    public GameObject headPrefab;
    public class HandMessage : MessageBase
    {
        public byte[] serializedHand;
        public NetworkInstanceId netID;
        public int id;
        public long frameID;
    }
    public class VRHeadMessage : MessageBase
    {
        public Vector3 HeadPos;
        public Quaternion HeadRot;
        public int ID;
        public NetworkInstanceId netId;
    }
    public class RemoteObjectSync
    {
        public Leap.Hand LHand;
        public Leap.Hand RHand;
        public Transform head;
        public float HeadFrameId;
    };
    private Dictionary<NetworkInstanceId, RemoteObjectSync> RemoteSyncedObjectsDict = new Dictionary<NetworkInstanceId, RemoteObjectSync>();
    public Dictionary<NetworkInstanceId, RemoteObjectSync> PubRemoteSyncedObjectsDict {
        get {
            return RemoteSyncedObjectsDict;
        }
    }

    //public Dictionary<int, NetworkInstanceId> networkAssociationDict = new Dictionary<int, NetworkInstanceId>();
    //public Dictionary<int, Transform> heads = new Dictionary<int, Transform>();
    public NetworkClient handClient;

    private float sendRateCheckHead;
    private float sendTimeLastHand;
    private long leftFrameID = 0;
    private long rightFrameID = 0;

    public bool leftHandTracking;
    public bool rightHandTracking;

    public byte[] lastSendLeftHand;
    public byte[] lastSendRightHand;
    public Vector3 lastSendHeadPos = Vector3.zero;
    public Quaternion lastSendHeadRot = Quaternion.identity;
    public struct vectorHandByte
    {
        public byte[] serializedHand;
        public NetworkInstanceId clientID;

    }
    // public class SharedHand : SyncListStruct<vectorHandByte>{}

    // public  SharedHand sharedHandList = new SharedHand();
    public Leap.Hand debugHand;

    [SerializeField]
    [OnEditorChange("leapProvider")]
    private LeapProvider _leapProvider;
    public LeapProvider leapProvider {
        get { return _leapProvider; }
        set {
            if (_leapProvider != null)
            {

                _leapProvider.OnUpdateFrame -= OnUpdateFrame;
            }

            _leapProvider = value;

            if (_leapProvider != null)
            {

                _leapProvider.OnUpdateFrame += OnUpdateFrame;
            }
        }
    }

    public void OnUpdateFrame(Leap.Frame frame)
    {
        if (!(Time.time - sendTimeLastHand > 0.033f))
        {
            //Debug.Log("Wiating to send hand");
            return;
        }
        sendTimeLastHand = Time.time;
        //Debug.Log("OnUpdateFrame Delegate");
        //Debug.Log(isClient.ToString() + ":Client and Server: " + isServer.ToString()+ "\tis local:"+ isLocalPlayer.ToString());
        leftHandTracking = false;
        rightHandTracking = false;

        if (frame != null && (SceneStateManager.Instance.MyState == ClientState.CLIENT || SceneStateManager.Instance.MyState == ClientState.HOST))
        {

            //Debug.Log(frame.Id);
            //Debug.Log(frame.Hands.Count);
            Leap.Hand leftHand = frame.Hands.Find(i => i.IsLeft == true);
            Leap.Hand rightHand = frame.Hands.Find(i => i.IsLeft == false);
            VectorHand_32 vHand = new VectorHand_32();
            HandMessage msg = new HandMessage();
            NetworkInstanceId netid_ = NetworkInstanceId.Invalid;
            if (SceneStateManager.Instance.localPlayer != null)
            {
                netid_ = SceneStateManager.Instance.localPlayer.GetComponent<NetworkTransform>().netId;
            }
            msg.netID = netid_;
            if (leftHand != null)
            {
                byte[] temp = new byte[vHand.numBytesRequired];
                vHand.Encode(leftHand);
                vHand.FillBytes(temp);
                msg.id = 1;
                msg.serializedHand = temp;
                msg.frameID = leftFrameID++;
                //Debug.Log(SceneStateManager.Instance.ThisClient);
                //LeftVectorHand = vHand;
                leftHandTracking = true;
                lastSendLeftHand = temp;
                SceneStateManager.Instance.ThisClient.SendByChannel(NetworkMessageType.uploadHand, msg, 2); //TODO DAVID

            }
            if (rightHand != null)
            {
                byte[] temp = new byte[vHand.numBytesRequired];
                vHand.Encode(rightHand);
                vHand.FillBytes(temp);
                msg.id = 0;
                msg.serializedHand = temp;
                msg.frameID = rightFrameID++;
                //Debug.Log(SceneStateManager.Instance.ThisClient);
                //RightVectorHand = vHand;
                rightHandTracking = true;
                lastSendRightHand = temp;
                SceneStateManager.Instance.ThisClient.SendByChannel(NetworkMessageType.uploadHand, msg, 2); //TODO DAVID

            }

        }
    }


    public bool FindLocalLeap()
    {
        //Debug.Log("Looking for the local Leap");
        foreach (LeapProvider lp in FindObjectsOfType<LeapProvider>())
        {
            if (lp.transform.parent.GetComponentInParent<NetworkIdentity>() != null)
            {
                if (lp.transform.parent.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
                {

                    _leapProvider = lp;
                    break;
                }
            }
            else { _leapProvider = null; }
        }

        if (_leapProvider == null)
        {
            return false;
            // _leapProvider = Hands.Provider;
        }

        _leapProvider.OnUpdateFrame -= OnUpdateFrame;
        _leapProvider.OnUpdateFrame += OnUpdateFrame;
        Debug.Log("Updated DelegateFunctions");
        return true;


    }

    void OnDisable()
    {
        if (_leapProvider != null)
            _leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }


    private void Awake()
    {
        VectorHand_32 vHand = new VectorHand_32();
        lastSendLeftHand = new byte[vHand.numBytesRequired];
        lastSendRightHand = new byte[vHand.numBytesRequired];
    }
    void Start()
    {


    }
    public void ReciveOtherHands(NetworkMessage msg)
    {
        
        HandMessage newHand = msg.ReadMessage<HandMessage>();
        if (NetworkInstanceId.Invalid == newHand.netID)
        {
            Debug.Log("Recevied Invalid NetId");
            return;
        }
        if (!RemoteSyncedObjectsDict.ContainsKey(newHand.netID))
        {
            AddNewRemoteSyncedObjects(newHand.netID);
        }
        if (!RemoteSyncedObjectsDict.ContainsKey(newHand.netID))
        {
            return;
        }
        VectorHand_32 vHand = new VectorHand_32();
        Leap.Hand result = new Leap.Hand();
        int offset = 0;
        vHand.ReadBytes(newHand.serializedHand, ref offset);
        vHand.Decode(result);
        if (result.IsLeft)
        {
            RemoteSyncedObjectsDict[newHand.netID].LHand = result;
            RemoteSyncedObjectsDict[newHand.netID].LHand.FrameId = newHand.frameID;
        }
        else
        {
            RemoteSyncedObjectsDict[newHand.netID].RHand = result;
            RemoteSyncedObjectsDict[newHand.netID].RHand.FrameId = newHand.frameID;
        }
        debugHand = result;
    }
    public void RecieveOtherVRHead(NetworkMessage msg)
    {

        
        VRHeadMessage newHead = msg.ReadMessage<VRHeadMessage>();
        if (NetworkInstanceId.Invalid == newHead.netId)
        {
            Debug.Log("Recevied Invalid NetId");
            return;
        }
        if (!RemoteSyncedObjectsDict.ContainsKey(newHead.netId))
        {
            AddNewRemoteSyncedObjects(newHead.netId);
        }
        if (!RemoteSyncedObjectsDict.ContainsKey(newHead.netId))
        {
            return;
        }
        if (RemoteSyncedObjectsDict[newHead.netId].head != null)
        {
            RemoteSyncedObjectsDict[newHead.netId].head.position = Vector3.Lerp(RemoteSyncedObjectsDict[newHead.netId].head.position, newHead.HeadPos, 0.5f);
            RemoteSyncedObjectsDict[newHead.netId].head.rotation = Quaternion.Lerp(RemoteSyncedObjectsDict[newHead.netId].head.rotation, newHead.HeadRot, 0.5f);
        }
        else
        {
            Transform tran = Instantiate(headPrefab).transform;
            RemoteSyncedObjectsDict[newHead.netId].head = tran;
            RemoteSyncedObjectsDict[newHead.netId].head.position = newHead.HeadPos;
            RemoteSyncedObjectsDict[newHead.netId].head.rotation = newHead.HeadRot;
        }
        if (RemoteSyncedObjectsDict[newHead.netId].head.parent == null)
        {
            GameObject go = ClientScene.FindLocalObject(newHead.netId);
            if (go != null)
            {
                RemoteSyncedObjectsDict[newHead.netId].head.parent = go.transform;
            }
        }
    }

    void AddNewRemoteSyncedObjects(NetworkInstanceId netId)
    {
        lock (RemoteSyncedObjectsDict)
        {
            RemoteObjectSync obj = new RemoteObjectSync();
            obj.head = null;
            obj.LHand = null;
            obj.RHand = null;
            obj.HeadFrameId = 0;
            RemoteSyncedObjectsDict.Add(netId, obj);

        }
    }


    private void Update()
    {

        if (SceneStateManager.Instance.ActionState == ActionState.DRIVE|| SceneStateManager.Instance.ActionState == ActionState.PREDRIVE || SceneStateManager.Instance.ActionState == ActionState.READY || SceneStateManager.Instance.ActionState == ActionState.QUESTIONS|| SceneStateManager.Instance.ActionState == ActionState.POSTQUESTIONS)
        {
            sendRateCheckHead += Time.deltaTime;
            if (sendRateCheckHead > 0.033f)
            {
                sendRateCheckHead = 0;
                if (Camera.main != null)
                {
                    Vector3 pos = Camera.main.transform.position;// + InputTracking.GetLocalPosition(XRNode.Head);
                    Quaternion rot = Camera.main.transform.rotation;//* InputTracking.GetLocalRotation(XRNode.Head);
                    lastSendHeadPos = pos;
                    lastSendHeadRot = rot;
                    NetworkInstanceId netid_ = NetworkInstanceId.Invalid;
                    if (SceneStateManager.Instance.localPlayer != null)
                    {
                        netid_ = SceneStateManager.Instance.localPlayer.GetComponent<NetworkTransform>().netId;
                    }
                    VRHeadMessage msg = new VRHeadMessage
                    {
                        HeadPos = pos,
                        HeadRot = rot,
                        netId = netid_
                    };

                    SceneStateManager.Instance.ThisClient.SendByChannel(NetworkMessageType.uploadVRHead, msg, 3);
                     //Debug.Log("Send a headPosition");
                }
            }
            else
            {
                // Debug.Log("Wiating to send head");
            }
        }
        if (_leapProvider == null)
        {

            if (SceneStateManager.Instance.MyState == ClientState.CLIENT || SceneStateManager.Instance.MyState == ClientState.HOST)
            {
                if (FindLocalLeap())
                {
                    Debug.Log("onClinet registering Download Hand");
                    SceneStateManager.Instance.ThisClient.connection.RegisterHandler(NetworkMessageType.DownloadHand, ReciveOtherHands);

                }
            }
        }
        if (debugHand != null)
        {
            DebugHand.GetComponent<DebugHand>().SetLeapHand(debugHand);
            DebugHand.GetComponent<DebugHand>().UpdateHand();
            Debug.DrawLine(debugHand.PalmPosition.ToVector3(), Vector3.zero, Color.red);
        }



    }
}

