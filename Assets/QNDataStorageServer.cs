using System.Collections;
using System.Collections.Generic;
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


       ParticipantOrder po =  ConnectionAndSpawing.Singleton.GetParticipantOrder(senderclientid);
        messagepayload.ReadValueSafe(out string message); 
        Debug.Log(message);

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
