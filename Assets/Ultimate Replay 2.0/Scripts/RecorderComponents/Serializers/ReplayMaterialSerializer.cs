using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Serializers
{
    public class ReplayMaterialSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayMaterialSerializeFlags : byte
        {
            None = 0,
            SharedMaterial = 1 << 0,
            Color = 1 << 1,
            MainTextureOffset = 1 << 2,
            MainTextureScale = 1 << 3,
            DoubleSidedGlobalIllumination = 1 << 4,
            GlobalIlluminationFlags = 1 << 5,
        }

        // Private
        [ReplayTextSerialize("Serialize Flags")]
        private ReplayMaterialSerializeFlags serializeFlags = 0;
        [ReplayTextSerialize("Color")]
        private Color color = Color.white;
        [ReplayTextSerialize("Main Texture Offset")]
        private Vector2 mainTextureOffset = Vector2.zero;
        [ReplayTextSerialize("Main Texture Scale")]
        private Vector2 mainTextureScale = Vector2.zero;
        [ReplayTextSerialize("Double Sided Global Illumination")]
        private bool doubleSidedGlobalIllumination = false;
        [ReplayTextSerialize("Global Illumination Flags")]
        private MaterialGlobalIlluminationFlags globalIlluminationFlags = 0;

        // Properties
        public ReplayMaterialSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        public Vector2 MainTextureOffset
        {
            get { return mainTextureOffset; }
            set { mainTextureOffset = value; }
        }

        public Vector2 MainTextureScale
        {
            get { return mainTextureScale; }
            set { mainTextureScale = value; }
        }

        public bool DoubleSidedGlobalIllumination
        {
            get { return doubleSidedGlobalIllumination; }
            set { doubleSidedGlobalIllumination = value; }
        }

        public MaterialGlobalIlluminationFlags GlobalIlluminationFlags
        {
            get { return globalIlluminationFlags; }
            set { globalIlluminationFlags = value; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((byte)serializeFlags);

            // Color - store as 32-bit color to save space
            if ((serializeFlags & ReplayMaterialSerializeFlags.Color) != 0)
                state.Write((Color32)color);

            // Texture offset
            if ((serializeFlags & ReplayMaterialSerializeFlags.MainTextureOffset) != 0)
                state.Write(mainTextureOffset);

            // Texture scale
            if ((serializeFlags & ReplayMaterialSerializeFlags.MainTextureScale) != 0)
                state.Write(mainTextureScale);

            // Double sided GI
            if ((serializeFlags & ReplayMaterialSerializeFlags.DoubleSidedGlobalIllumination) != 0)
                state.Write(doubleSidedGlobalIllumination);

            // Global illumination flags
            if ((serializeFlags & ReplayMaterialSerializeFlags.GlobalIlluminationFlags) != 0)
                state.Write((int)globalIlluminationFlags);
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayMaterialSerializeFlags)state.ReadByte();

            // Color
            if ((serializeFlags & ReplayMaterialSerializeFlags.Color) != 0)
                color = state.ReadColor32();

            // Texture offset
            if ((serializeFlags & ReplayMaterialSerializeFlags.MainTextureOffset) != 0)
                mainTextureOffset = state.ReadVec2();

            // Texture scale
            if ((serializeFlags & ReplayMaterialSerializeFlags.MainTextureScale) != 0)
                mainTextureScale = state.ReadVec2();

            // Double sided GI
            if ((serializeFlags & ReplayMaterialSerializeFlags.DoubleSidedGlobalIllumination) != 0)
                doubleSidedGlobalIllumination = state.ReadBool();

            // Global illumination flags
            if ((serializeFlags & ReplayMaterialSerializeFlags.GlobalIlluminationFlags) != 0)
                globalIlluminationFlags = (MaterialGlobalIlluminationFlags)state.ReadInt32();
        }

        public void Reset()
        {
            serializeFlags = 0;
            color = Color.white;
            mainTextureOffset = Vector2.zero;
            mainTextureScale = Vector2.zero;
            doubleSidedGlobalIllumination = false;
            globalIlluminationFlags = 0;
        }
    }
}
