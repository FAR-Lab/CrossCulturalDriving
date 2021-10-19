using System.IO;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;
#endif


namespace UltimateReplay.Storage
{
    internal static class ReplayStreamSerializationUtility
    {
        // Methods
        public static void StreamSerialize<T>(T item, BinaryWriter writer) where T : IReplayStreamSerialize
        {
            // Serialize the type
            item.OnReplayStreamSerialize(writer);
        }

        public static void StreamDeserialize<T>(ref T item, BinaryReader reader) where T : IReplayStreamSerialize
        {
            // Deserialize the type
            item.OnReplayStreamDeserialize(reader);
        }

#if ULTIMATEREPLAY_JSON
        //public static void StreamSerialize<T>(T item, JsonWriter writer) where T : IReplayStreamSerialize
        //{
        //    item.OnReplayStreamSerialize(writer);
        //}

        //public static void StreamDeserialize<T>(ref T item, JsonReader reader) where T : IReplayStreamSerialize
        //{
        //    // Deserialize the type
        //    item.OnReplayStreamDeserialize(reader);
        //}
#endif
    }
}
