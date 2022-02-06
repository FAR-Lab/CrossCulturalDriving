using System.Collections;
using System.Collections.Generic;
using System.Text;
using Oculus.Platform;
using Rerun;
using Unity.Netcode;
using UnityEngine;

public class QNDataStorageServer : MonoBehaviour {
    private RerunManager _rerunManager;

    // Start is called before the first frame update
    void Start() {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(QNLogger.qnMessageName,
            DataStorage);
        _rerunManager = FindObjectOfType<RerunManager>();
    }

    private void DataStorage(ulong senderclientid, FastBufferReader messagepayload) {
        if (_rerunManager == null) { Debug.Log(messagepayload.Length); }
        else {
            ParticipantOrder po = ConnectionAndSpawing.Singleton.GetParticipantOrderClientId(senderclientid);
            string fullPath = _rerunManager.GetCurrentFilePath()
                              + "Scenario-" + ConnectionAndSpawing.Singleton.GetLoadedScene() + '_'
                              + "po-" + po.ToString() + '_'
                              + "Session-" + _rerunManager.GetRecordingFolder() + '_'
                              + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";

            messagepayload.ReadNetworkSerializable<QNResultMessage>(out QNResultMessage value);
            System.IO.File.WriteAllText(fullPath, value.message);
        }
    }

    // Update is called once per frame
    void Update() { }
}

public struct QNResultMessage : INetworkSerializable {
    public string message;


    public int GetSize() {
        UnicodeEncoding unicode = new UnicodeEncoding();
        return unicode.GetByteCount(message) + 4; // plus 4 for the int
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        UnicodeEncoding unicode = new UnicodeEncoding();

        int ByteLength = 0;
        byte[] temp;
        if (!serializer.IsReader) { ByteLength = unicode.GetByteCount(message); }

        serializer.SerializeValue(ref ByteLength);

        if (serializer.IsReader) {
            temp = new byte[ByteLength];
            Debug.Log("I allocated " + ByteLength);
        }
        else {
            temp = unicode.GetBytes(message);
            Debug.Log("I send allocated:" + ByteLength + " my array has this many bytes: " + temp.Length);
        }

        for (int n = 0; n < ByteLength; ++n) { serializer.SerializeValue(ref temp[n]); }

        if (serializer.IsReader) { message = unicode.GetString(temp); }
    }
}