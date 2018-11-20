using UnityEngine;
using UnityEngine.Networking;

class SpawnMessage : MessageBase
{
    public uint netId;
    //public Vector3 position;
    //public byte[] payload;

    // This method would be generated
    public override void Deserialize(NetworkReader reader)
    {
        netId = reader.ReadPackedUInt32();

    }

    // This method would be generated
    public override void Serialize(NetworkWriter writer)
    {
        writer.WritePackedUInt32(netId);
    }
}