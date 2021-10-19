using System.IO;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;
#endif

namespace UltimateReplay.Storage
{
    /// <summary>
    /// Interface that should be implemented by any types that can be serialized to a steam object.
    /// </summary>
    public interface IReplayStreamSerialize
    {
        // Methods
        /// <summary>
        /// Called by the replay system when the object should serialize its replay data into a binary target.
        /// </summary>
        /// <param name="writer">The writer object used to store data</param>
        void OnReplayStreamSerialize(BinaryWriter writer);

        /// <summary>
        /// Called by the replay system when the object should deserialize its replay data from a binary source.
        /// </summary>
        /// <param name="reader">The reader where the data is stored</param>
        void OnReplayStreamDeserialize(BinaryReader reader);

#if ULTIMATEREPLAY_JSON
        //void OnReplayStreamSerialize(JsonWriter writer);

        //void OnReplayStreamDeserialize(JsonReader reader);
#endif
    }
}
