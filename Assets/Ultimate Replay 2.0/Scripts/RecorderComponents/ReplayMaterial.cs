using System;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplayMaterial : ReplayRecordableBehaviour
    {
        // Types
        [Flags]
        public enum ReplayMaterialFlags
        {
            None = 0,
            SharedMaterial = 1 << 0,
            Color = 1 << 1,
            MainTextureOffset = 1 << 2,
            MainTextureScale = 1 << 3,
            DoubleSidedGlobalIllumination = 1 << 4,
            GlobalIlluminationFlags = 1 << 5,
            Interpolate = 1 << 6,
        }

        private struct ReplayMaterialData
        {
            // Public
            public Color color;
            public Vector2 mainTextureOffset;
            public Vector2 mainTextureScale;
            public bool doubleSidedGlobalIllumination;
            public MaterialGlobalIlluminationFlags globalIlluminationFlags;
        }

        // Private
        private static readonly ReplayMaterialSerializer sharedSerializer = new ReplayMaterialSerializer();
        private ReplayMaterialData lastMaterialData;
        private ReplayMaterialData targetMaterialData;
        private ReplayMaterialSerializer.ReplayMaterialSerializeFlags updateFlags = 0;

        // Public
        public Renderer observedRenderer = null;
        [HideInInspector]
        public ReplayMaterialFlags recordFlags = ReplayMaterialFlags.SharedMaterial | ReplayMaterialFlags.Color;
        [Tooltip("The index of the renderer material to record or '-1' if the main material should be used")]
        public int materialIndex = -1;

        // Methods
        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to audo-find renderer
            if (observedRenderer == null)
            {
                observedRenderer = GetComponent<Renderer>();
            }
        }

        public override void OnReplayReset()
        {
            // Reset data
            lastMaterialData = targetMaterialData;
        }

        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Check for any data
            if (updateFlags == 0)
                return;

            // Get the material
            Material material = GetTargetMaterial();

            // Check for error
            if (material == null)
                return;

            // Create update structure
            ReplayMaterialData updateData = targetMaterialData;

            // Check for interpolation
            if((recordFlags & ReplayMaterialFlags.Interpolate) != 0)
            {
                updateData.color = Color.Lerp(lastMaterialData.color, targetMaterialData.color, replayTime.Delta);
                updateData.mainTextureOffset = Vector2.Lerp(lastMaterialData.mainTextureOffset, targetMaterialData.mainTextureOffset, replayTime.Delta);
                updateData.mainTextureScale = Vector2.Lerp(lastMaterialData.mainTextureScale, targetMaterialData.mainTextureScale, replayTime.Delta);
            }

            // Update material properties
            // Color
            if((updateFlags & ReplayMaterialSerializer.ReplayMaterialSerializeFlags.Color) != 0) 
                material.color = updateData.color;

            // Main texture offset
            if ((updateFlags & ReplayMaterialSerializer.ReplayMaterialSerializeFlags.MainTextureOffset) != 0) 
                material.mainTextureOffset = updateData.mainTextureOffset;

            // Main texture scale
            if ((updateFlags & ReplayMaterialSerializer.ReplayMaterialSerializeFlags.MainTextureScale) != 0)
                material.mainTextureScale = updateData.mainTextureScale;

            // Double sided GI
            if((updateFlags & ReplayMaterialSerializer.ReplayMaterialSerializeFlags.DoubleSidedGlobalIllumination) != 0)
                material.doubleSidedGI = updateData.doubleSidedGlobalIllumination;

            // Global illumination flags
            if ((updateFlags & ReplayMaterialSerializer.ReplayMaterialSerializeFlags.GlobalIlluminationFlags) != 0)
                material.globalIlluminationFlags = updateData.globalIlluminationFlags;


            // Apply material changes - (Required when not using 'shared material' as a new instance is created
            if ((recordFlags & ReplayMaterialFlags.SharedMaterial) == 0)
                ApplyTargetMaterial(material);
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for observed renderer
            if (observedRenderer == null)
                return;

            // Get material
            Material material = GetTargetMaterial();

            // Check for error
            if (Application.isPlaying == true && material == null)
            {
                Debug.LogWarningFormat("Replay material will not be recorded because the observed renderer does not have a material at slot '{0}'", materialIndex);
                return;
            }

            // Set serializer values
            sharedSerializer.SerializeFlags = (ReplayMaterialSerializer.ReplayMaterialSerializeFlags)recordFlags;
            sharedSerializer.Color = material.color;
            sharedSerializer.MainTextureOffset = material.mainTextureOffset;
            sharedSerializer.MainTextureScale = material.mainTextureScale;
            sharedSerializer.DoubleSidedGlobalIllumination = material.doubleSidedGI;
            sharedSerializer.GlobalIlluminationFlags = material.globalIlluminationFlags;

            // Run the serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for observed renderer
            if (observedRenderer == null)
                return;

            // Update last values
            OnReplayReset();

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Fetch values
            updateFlags = sharedSerializer.SerializeFlags;
            targetMaterialData.color = sharedSerializer.Color;
            targetMaterialData.mainTextureOffset = sharedSerializer.MainTextureOffset;
            targetMaterialData.mainTextureScale = sharedSerializer.MainTextureScale;
            targetMaterialData.doubleSidedGlobalIllumination = sharedSerializer.DoubleSidedGlobalIllumination;
            targetMaterialData.globalIlluminationFlags = sharedSerializer.GlobalIlluminationFlags;

        }

        private Material GetTargetMaterial()
        {
            if(materialIndex == -1)
            {
                // Check for shared material
                if (Application.isPlaying == false || (recordFlags & ReplayMaterialFlags.SharedMaterial) != 0)
                    return observedRenderer.sharedMaterial;

                // Use non-shared material
                return observedRenderer.material;
            }
            else
            {
                // Try to get material at index
                if(materialIndex >= 0 && materialIndex < observedRenderer.sharedMaterials.Length)
                {
                    // Check for shared material
                    if (Application.isPlaying == false || (recordFlags & ReplayMaterialFlags.SharedMaterial) != 0)
                        return observedRenderer.sharedMaterials[materialIndex];

                    // Use non-shared material at index
                    return observedRenderer.materials[materialIndex];
                }
            }

            // Invalid material index
            return null;
        }

        private void ApplyTargetMaterial(Material material)
        {
            if (materialIndex == -1)
            {
                // Check for shared material
                if ((recordFlags & ReplayMaterialFlags.SharedMaterial) != 0)
                {
                    observedRenderer.sharedMaterial = material;
                }
                else
                {
                    // Use non-shared material
                    observedRenderer.material = material;
                }
            }
            else
            {
                // Try to get material at index
                if (materialIndex >= 0 && materialIndex < observedRenderer.sharedMaterials.Length)
                {
                    // Check for shared material
                    if ((recordFlags & ReplayMaterialFlags.SharedMaterial) != 0)
                    {
                        observedRenderer.sharedMaterials[materialIndex] = material;
                    }
                    else
                    {
                        // Use non-shared material at index
                        observedRenderer.materials[materialIndex] = material;
                    }
                }
            }
        }
    }
}