using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public struct BasicByteArraySender : INetworkSerializable
{
    public byte[] DataSendArray;

 

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        
        int length = 0;
        if (!serializer.IsReader)
        {
            length = DataSendArray.Length;
        }

        serializer.SerializeValue(ref length);

        if (serializer.IsReader)
        {
            DataSendArray = new byte[length];
        }

        for (int n = 0; n < length; ++n)
        {
            serializer.SerializeValue(ref DataSendArray[n]);
        }
    }
}