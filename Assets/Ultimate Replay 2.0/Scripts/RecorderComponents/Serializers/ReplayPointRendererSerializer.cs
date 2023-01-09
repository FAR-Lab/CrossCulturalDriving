using System;
using System.Collections.Generic;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Serializers
{
    public sealed class ReplayPointRendererSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayPointRendererSerializeFlags : byte
        {
            None = 0,
            LowPrecision = 1 << 1,
            HalfPrecisionCount = 1 << 2,
        }

        // Private
        private ReplayPointRendererSerializeFlags serializeFlags = 0;
        private List<Vector3> points = new List<Vector3>();

        // Properties
        public ReplayPointRendererSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        public IList<Vector3> Points
        {
            get { return points; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((byte)serializeFlags);

            // Write count
            if ((serializeFlags & ReplayPointRendererSerializeFlags.HalfPrecisionCount) != 0)
            {
                state.Write((ushort)points.Count);
            }
            else
            {
                state.Write((uint)points.Count);
            }

            // Write all points
            for(int i = 0; i < points.Count; i++)
            {
                if((serializeFlags & ReplayPointRendererSerializeFlags.LowPrecision) != 0)
                {
                    // Write half precision
                    state.WriteLowPrecision(points[i]);
                }
                else
                {
                    // Write full precision
                    state.Write(points[i]);
                }
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Clear points
            Reset();

            // Read flags
            serializeFlags = (ReplayPointRendererSerializeFlags)state.ReadByte();

            int count = 0;

            // Read count
            if((serializeFlags & ReplayPointRendererSerializeFlags.HalfPrecisionCount) != 0)
            {
                count = state.ReadUInt16();
            }
            else
            {
                count = (int)state.ReadUInt32();
            }

            // Read all points
            for(int i = 0; i < count; i++)
            {
                if((serializeFlags & ReplayPointRendererSerializeFlags.LowPrecision) != 0)
                {
                    // Read half precision
                    points.Add(state.ReadVec3LowPrecision());
                }
                else
                {
                    // Read full precision
                    points.Add(state.ReadVec3());
                }
            }
        }

        public void Reset()
        {
            points.Clear();
        }
    }
}
