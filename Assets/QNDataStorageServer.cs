using System.Collections;
using System.Collections.Generic;
using System.Text;
using Oculus.Platform;
using Unity.Netcode;
using UnityEngine;

public class QNDataStorageServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(QNLogger.qnMessageName,DataStorage);
    }

    private void DataStorage(ulong senderclientid, FastBufferReader messagepayload) {


       ParticipantOrder po =  ConnectionAndSpawing.Singleton.GetParticipantOrderClientId(senderclientid);
       byte[] value = new byte[] { };
     //  messagepayload.ReadBytesSafe(value); 
        
       // Debug.Log();

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
