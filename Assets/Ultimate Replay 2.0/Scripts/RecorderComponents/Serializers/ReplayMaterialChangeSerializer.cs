using System;
using System.Collections.Generic;
using System.Linq;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Serializers
{
    public sealed class ReplayMaterialChangeSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayMaterialChangeSerializeFlags : ushort
        {
            SharedMaterial = 1 << 0,
            AllMaterials = 1 << 1,
            _8BitIndex = 1 << 2,
            _16BitIndex = 1 << 3,
            _32BitIndex = 1 << 4,
        }

        // Private
        [ReplayTextSerialize("Serialize Flags")]
        private ReplayMaterialChangeSerializeFlags serializeFlags = 0;
        [ReplayTextSerialize("Material Indexes")]
        private int[] materialIndexes = new int[1];

        // Properties
        public int MaterialIndex
        {
            get { return materialIndexes[0]; }
        }

        public int[] MaterialIndexes
        {
            get { return materialIndexes; }
        }

        public ReplayMaterialChangeSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Write flags
            state.Write((ushort)serializeFlags);

            // Write material indexes
            if((serializeFlags & ReplayMaterialChangeSerializeFlags.AllMaterials) != 0)
            {
                // Write the array size
                WriteMaterialInteger(state, materialIndexes.Length);

                // Write all indexes
                for (int i = 0; i < materialIndexes.Length; i++)
                    WriteMaterialInteger(state, materialIndexes[i]);
            }
            else
            {
                // Write the main material
                WriteMaterialInteger(state, materialIndexes[0]);
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayMaterialChangeSerializeFlags)state.ReadUInt16();

            // Read material indexes
            if((serializeFlags & ReplayMaterialChangeSerializeFlags.AllMaterials) != 0)
            {
                // Read the size
                int size = ReadMaterialInteger(state);

                // Allocate array if required
                if (materialIndexes.Length != size)
                    Array.Resize(ref materialIndexes, size);

                // Read all indexes
                for (int i = 0; i < size; i++)
                    materialIndexes[i] = ReadMaterialInteger(state);
            }
            else
            {
                // Allocate array if required
                if (materialIndexes.Length != 1)
                    Array.Resize(ref materialIndexes, 1);

                // Read the main material
                materialIndexes[0] = ReadMaterialInteger(state);
            }
        }

        public void SetActiveMaterial(IList<Material> possibleMaterials, Material activeMaterial)
        {
            // Resize array if required
            if (materialIndexes.Length != 1)
                Array.Resize(ref materialIndexes, 1);

            // Find index (-1 on error)
            materialIndexes[0] = GetMaterialIndex(possibleMaterials, activeMaterial);// possibleMaterials.IndexOf(activeMaterial);

            // Update flags to store indexes in lowest possible size
            UpdateIndexSizeFlags(possibleMaterials.Count);

            // Check if material was available
            if (Application.isPlaying == true && materialIndexes[0] == -1)
                Debug.LogWarningFormat("Replay material change cannot be recorded. The specified '{0}' does not exist in the 'Available Materials' collection. The default material will be used during playback!", activeMaterial);
        }

        public void SetActiveMaterials(IList<Material> possibleMaterials, Material[] activeMaterials)
        {
            // Resize array if required
            if (materialIndexes.Length != activeMaterials.Length)
                Array.Resize(ref materialIndexes, activeMaterials.Length);

            // Find all indexes
            for (int i = 0; i < activeMaterials.Length; i++)
            {
                materialIndexes[i] = GetMaterialIndex(possibleMaterials, activeMaterials[i]);//  possibleMaterials.IndexOf(activeMaterials[i]);

                // Check if material was available
                if (Application.isPlaying == true && materialIndexes[i] == -1)
                    Debug.LogWarningFormat("Replay material change cannot be recorded. The specified '{0}' does not exist in the 'Available Materials' collection. The default material will be used during playback!", activeMaterials[i]);
            }

            // Update flags to store indexes in lowest possible size
            UpdateIndexSizeFlags(possibleMaterials.Count);
        }

        public Material GetActiveMaterial(IList<Material> possibleMaterials)
        {
            // Check if index is within bounds
            if (materialIndexes[0] >= 0 && materialIndexes[0] < possibleMaterials.Count)
                return possibleMaterials[materialIndexes[0]];

            // Error value
            return null;
        }

        public int GetActiveMaterials(IList<Material> possibleMaterials, Material[] results)
        {
            if (materialIndexes.Length != results.Length)
                throw new ArgumentException("The specified results array size must match 'MaterialIndexes.Length'");

            int successCount = 0;

            for(int i = 0; i < materialIndexes.Length; i++)
            {
                if(materialIndexes[i] >= 0 && materialIndexes[i] < possibleMaterials.Count)
                {
                    // Get the material index
                    results[i] = possibleMaterials[materialIndexes[i]];

                    // Update success count
                    successCount++;
                }
                else
                {
                    // Error
                    results[i] = null;
                }
            }

            return successCount;
        }

        private void WriteMaterialInteger(ReplayState state, int value)
        {
            if((serializeFlags & ReplayMaterialChangeSerializeFlags._8BitIndex) != 0)
            {
                state.Write((byte)value);
            }
            else if((serializeFlags & ReplayMaterialChangeSerializeFlags._16BitIndex) != 0)
            {
                state.Write((ushort)value);
            }
            else
            {
                state.Write(value);
            }
        }

        private int ReadMaterialInteger(ReplayState state)
        {
            if ((serializeFlags & ReplayMaterialChangeSerializeFlags._8BitIndex) != 0)
            {
                return state.ReadByte();
            }
            else if ((serializeFlags & ReplayMaterialChangeSerializeFlags._16BitIndex) != 0)
            {
                return state.ReadUInt16();
            }

            return state.ReadInt32();
        }

        private int GetMaterialIndex(IList<Material> possibleMaterials, Material activeMaterial)
        {
            string activeName = activeMaterial.name;

            if (activeName.Contains(" (Instance)") == true)
                activeName = activeName.Replace(" (Instance)", string.Empty);

            for (int i = 0; i < possibleMaterials.Count; i++)
            {
                // Use name matching because Unity creates material instances
                if (possibleMaterials[i].name.StartsWith(activeName) == true)
                    return i;
            }
            return -1;
        }

        private void UpdateIndexSizeFlags(int arraySize)
        {
            // Clear flags
            serializeFlags &= ~ReplayMaterialChangeSerializeFlags._8BitIndex;
            serializeFlags &= ~ReplayMaterialChangeSerializeFlags._16BitIndex;
            serializeFlags &= ~ReplayMaterialChangeSerializeFlags._32BitIndex;

            // Update flags
            if (arraySize < byte.MaxValue) serializeFlags |= ReplayMaterialChangeSerializeFlags._8BitIndex;
            else if (arraySize < ushort.MaxValue) serializeFlags |= ReplayMaterialChangeSerializeFlags._16BitIndex;
            else serializeFlags |= ReplayMaterialChangeSerializeFlags._32BitIndex;
        }
    }
}
