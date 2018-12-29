using UnityEngine;
using UnityEngine.Assertions;
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

public class RemoteHandManager :  MonoBehaviour {

    public Transform DebugHand;
    public class MessageType {
        public static short uploadHand = MsgType.Highest + 1;
        public static short DownloadHand = MsgType.Highest + 2;
    };
    public class HandMessage : MessageBase
    {
        public byte[] serializedHand;
        public int id;
    }
    public Dictionary<int, Leap.Hand> networkHands = new Dictionary<int, Leap.Hand>();
    public NetworkClient handClient;

    //public event Action<Dictionary<int, Leap.Hand>> UpdateNetworkedHands;

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
    public LeapProvider leapProvider
    {
        get { return _leapProvider; }
        set
        {
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
        //Debug.Log("OnUpdateFrame Delegate");
        //Debug.Log(isClient.ToString() + ":Client and Server: " + isServer.ToString()+ "\tis local:"+ isLocalPlayer.ToString());
        if (frame != null && (SceneStateManager.Instance.MyState==ClientState.CLIENT || SceneStateManager.Instance.MyState == ClientState.HOST))
        {
            //Debug.Log(frame.Id);
            //Debug.Log(frame.Hands.Count);
            Leap.Hand leftHand = frame.Hands.Find(i => i.IsLeft == true);
            Leap.Hand rightHand = frame.Hands.Find(i => i.IsLeft == false);
            VectorHand_32 vHand = new VectorHand_32();
            if (leftHand != null) {
                HandMessage msg = new HandMessage();
                    byte[] temp = new byte[vHand.numBytesRequired];
                    vHand.Encode(leftHand);
                    vHand.FillBytes(temp);
                msg.id = 1;
                msg.serializedHand = temp;
                //Debug.Log(SceneStateManager.Instance.ThisClient);
                SceneStateManager.Instance.ThisClient.Send(MessageType.uploadHand, msg);
                
            }
            if (rightHand != null)
            {
                HandMessage msg = new HandMessage();
                byte[] temp = new byte[vHand.numBytesRequired];
                vHand.Encode(rightHand);
                vHand.FillBytes(temp);
                msg.id = 0;
                msg.serializedHand = temp;
                //Debug.Log(SceneStateManager.Instance.ThisClient);
                SceneStateManager.Instance.ThisClient.Send(MessageType.uploadHand, msg);

            }

        }
    }
    
    
    public bool FindLocalLeap()
    {
        Debug.Log("Looking for the local Leap");
        foreach(LeapProvider lp in FindObjectsOfType<LeapProvider>())
        {

            if (lp.transform.parent.GetComponentInParent<NetworkIdentity>().isLocalPlayer)
            {

                _leapProvider = lp;
                break;
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
        if(_leapProvider!=null)
        _leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }


  
    /** Popuates the ModelPool with the contents of the ModelCollection */
    void Start()
    {

      
    }
    public void  ReciveOtherHands(NetworkMessage msg) {
        HandMessage newHand = msg.ReadMessage<HandMessage>();
        Debug.Log("got a new remote hand");
        VectorHand_32 vHand = new VectorHand_32();
        Leap.Hand result = new Leap.Hand();
        int offset = 0;
        vHand.ReadBytes(newHand.serializedHand,ref offset);
        vHand.Decode(result);
        networkHands[newHand.id] = result;
        debugHand = result;


    }


    private void Update()
    {

       
        if (_leapProvider == null)
        {
           
            if (SceneStateManager.Instance.MyState==ClientState.CLIENT || SceneStateManager.Instance.MyState == ClientState.HOST)
            {
                if (FindLocalLeap())
                {
                    Debug.Log("onClinet registering Download Hand");
                    SceneStateManager.Instance.ThisClient.connection.RegisterHandler(MessageType.DownloadHand, ReciveOtherHands);
                }
            }
        }
        if (debugHand!=null) {
            DebugHand.GetComponent<DebugHand>().SetLeapHand(debugHand);
            DebugHand.GetComponent<DebugHand>().UpdateHand();
            Debug.DrawLine(debugHand.PalmPosition.ToVector3(), Vector3.zero, Color.red);
         }
        
    }
}

