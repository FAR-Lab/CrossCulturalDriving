using System;
using System.Collections.Generic;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplayMaterialChange : ReplayRecordableBehaviour
    {
        // Types
        [Flags]
        public enum ReplayMaterialChangeFlags
        {
            None = 0,
            SharedMaterial = 1 << 0,
            AllMaterials = 1 << 1,
        }

        // Private
        private ReplayMaterialChangeSerializer serializer = new ReplayMaterialChangeSerializer();
        private Material[] activeMaterials = null;

        // Public
        public Renderer observedRenderer = null;
        public Material defaultMaterial;
        public List<Material> availableMaterials = new List<Material>();
        [HideInInspector]
        public ReplayMaterialChangeFlags recordFlags = ReplayMaterialChangeFlags.SharedMaterial;

        // Methods
        public override void Reset()
        {
            // Call base method
            base.Reset();

            // Try to audo-find material
            if(observedRenderer == null)
            {
                observedRenderer = GetComponent<Renderer>();

                if (observedRenderer != null)
                {
                    defaultMaterial = observedRenderer.sharedMaterial;

                    // Initialize available materials array
                    if (availableMaterials.Count == 0)
                        availableMaterials.Add(defaultMaterial);
                }
            }
        }

        public int GetAssignedMaterialIndex(int slot = -1)
        {
            // Check for observed renderer
            if (observedRenderer == null)
                return -1;

            Material material = null;

            // Check for shared
            if((recordFlags & ReplayMaterialChangeFlags.SharedMaterial) != 0)
            {
                if (slot == -1)
                {
                    material = observedRenderer.sharedMaterial;
                }
                else
                {
                    material = observedRenderer.sharedMaterials[slot];
                }
            }
            else
            {
                if (slot == -1)
                {
                    material = observedRenderer.material;
                }
                else
                {
                    material = observedRenderer.materials[slot];
                }
            }

            // Check for error
            if (material == null)
                return -1;

            // Try to find material
            return availableMaterials.IndexOf(material);
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for observed renderer
            if (observedRenderer == null)
                return;

            // Set serializer flags
            serializer.SerializeFlags = (ReplayMaterialChangeSerializer.ReplayMaterialChangeSerializeFlags)recordFlags;

            // Check for all materials
            if((recordFlags & ReplayMaterialChangeFlags.AllMaterials) == 0)
            {
                // Check for shared materials
                if(Application.isPlaying == false || (recordFlags & ReplayMaterialChangeFlags.SharedMaterial) != 0)
                {
                    // Set serializer materials
                    serializer.SetActiveMaterial(availableMaterials, observedRenderer.sharedMaterial);
                }
                else
                {
                    // Set serializer material (non-shared)
                    serializer.SetActiveMaterial(availableMaterials, observedRenderer.material);
                }
            }
            else
            {
                // Check for shared materials
                if(Application.isPlaying == false || (recordFlags & ReplayMaterialChangeFlags.SharedMaterial) != 0)
                {
                    // Set serialize materials (multiple)
                    serializer.SetActiveMaterials(availableMaterials, observedRenderer.sharedMaterials);
                }
                else
                {
                    // Set serializer materials (multiple, non-shared)
                    serializer.SetActiveMaterials(availableMaterials, observedRenderer.materials);
                }
            }

            // Run serializer
            serializer.OnReplaySerialize(state);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            // Check for observed renderer
            if (observedRenderer == null)
                return;

            // Run serializer
            serializer.OnReplayDeserialize(state);

            // Get flags
            ReplayMaterialChangeFlags flags = (ReplayMaterialChangeFlags)serializer.SerializeFlags;

            // Check for all materials
            if((flags & ReplayMaterialChangeFlags.AllMaterials) == 0)
            {
                // Get the active material
                Material activeMaterial = serializer.GetActiveMaterial(availableMaterials);

                // Check for error - fallback to default
                if (activeMaterial == null)
                    activeMaterial = defaultMaterial;

                // Check for shared materials
                if ((flags & ReplayMaterialChangeFlags.SharedMaterial) != 0)
                {
                    // Apply the material
                    observedRenderer.sharedMaterial = activeMaterial;
                }
                else
                {
                    // Apply the material (non-shared)
                    observedRenderer.material = activeMaterial;
                }
            }
            else
            {
                // Allocate array if required
                if (activeMaterials == null)
                {
                    activeMaterials = new Material[serializer.MaterialIndexes.Length];
                }
                else if (activeMaterials.Length != serializer.MaterialIndexes.Length)
                {
                    Array.Resize(ref activeMaterials, serializer.MaterialIndexes.Length);
                }

                // Get the active materials
                int validMaterialCount = serializer.GetActiveMaterials(availableMaterials, activeMaterials);

                // Check if any materials were not applied
                if(validMaterialCount != serializer.MaterialIndexes.Length)
                {
                    for(int i = 0; i < activeMaterials.Length; i++)
                    {
                        // Fallback to default material
                        if (activeMaterials[i] == null)
                            activeMaterials[i] = defaultMaterial;
                    }
                }

                // Check for shared 
                if((flags & ReplayMaterialChangeFlags.SharedMaterial) != 0)
                {
                    // Apply the material (multiple)
                    observedRenderer.sharedMaterials = activeMaterials;
                }
                else
                {
                    // Appl the material (multiple, non-shared)
                    observedRenderer.materials = activeMaterials;
                }
            }
        }
    }
}