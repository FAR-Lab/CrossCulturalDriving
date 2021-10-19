using System;
using System.Collections.Generic;
using UltimateReplay.Core;

namespace UltimateReplay.Serializers
{
    public class ReplayBlendShapeSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayBlendShapeSerializeFlags : byte
        {
            None = 0,
            LowPrecision = 1 << 1,
            HalfPrecisionCount = 1 << 2,
        }

        // Private
        private ReplayBlendShapeSerializeFlags serializeFlags = 0;
        private List<float> blendWeights = new List<float>();

        // Properties
        public ReplayBlendShapeSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        public IList<float> BlendWeights
        {
            get { return blendWeights; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((byte)serializeFlags);

            // Write count
            if((serializeFlags & ReplayBlendShapeSerializeFlags.HalfPrecisionCount) != 0)
            {
                state.Write((ushort)blendWeights.Count);
            }
            else
            {
                state.Write((uint)blendWeights.Count);
            }

            // Write all weights
            for(int i = 0; i < blendWeights.Count; i++)
            {
                if((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecision) != 0)
                {
                    // Write half precision
                    state.WriteLowPrecision(blendWeights[i]);
                }
                else
                {
                    // Write full precision
                    state.Write(blendWeights[i]);
                }
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Reset serializer
            Reset();

            // Read flags
            serializeFlags = (ReplayBlendShapeSerializeFlags)state.ReadByte();

            int count = 0;

            // Read count
            if((serializeFlags & ReplayBlendShapeSerializeFlags.HalfPrecisionCount) != 0)
            {
                count = state.ReadUInt16();
            }
            else
            {
                count = (int)state.ReadUInt32();
            }

            // Read all weights
            for(int i = 0; i < count; i++)
            {
                if((serializeFlags & ReplayBlendShapeSerializeFlags.LowPrecision) != 0)
                {
                    // Read half precision
                    blendWeights.Add(state.ReadFloatLowPrecision());
                }
                else
                {
                    // Read full precision
                    blendWeights.Add(state.ReadFloat());
                }
            }
        }

        public void Reset()
        {
            blendWeights.Clear();
        }
    }
}
