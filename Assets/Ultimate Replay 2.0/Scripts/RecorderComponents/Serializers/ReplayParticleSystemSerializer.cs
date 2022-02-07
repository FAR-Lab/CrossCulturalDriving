using System;
using UltimateReplay.Core;

namespace UltimateReplay.Serializers
{
    public sealed class ReplayParticleSystemSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayParticleSystemSerializeFlags : ushort
        {
            None = 0,
            LowPrecision = 1 << 1,
        }

        // Private
        private ReplayParticleSystemSerializeFlags serializeFlags = 0;
        private uint randomSeed = 0;
        private float simulationTime = 0f;

        // Properties
        public ReplayParticleSystemSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        public uint RandomSeed
        {
            get { return randomSeed; }
            set { randomSeed = value; }
        }

        public float SimulationTime
        {
            get { return simulationTime; }
            set { simulationTime = value; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write((ushort)serializeFlags);

            // Write seed value in full precision
            state.Write(randomSeed);

            // Check for low precision
            if ((serializeFlags & ReplayParticleSystemSerializeFlags.LowPrecision) != 0)
            {
                // Write half precision
                state.WriteLowPrecision(simulationTime);
            }
            else
            {
                // Write full precision
                state.Write(simulationTime);
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read serialize flags
            serializeFlags = (ReplayParticleSystemSerializeFlags)state.ReadUInt16();

            // Read seed
            randomSeed = state.ReadUInt32();

            // Check for low precision
            if ((serializeFlags & ReplayParticleSystemSerializeFlags.LowPrecision) != 0)
            {
                // Read half precision
                simulationTime = state.ReadFloatLowPrecision();
            }
            else
            {
                // Read full precision
                simulationTime = state.ReadFloat();
            }
        }        
    }
}
