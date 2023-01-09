using System;
using UltimateReplay.Serializers;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A replay recorder component used to record and replay transform components.
    /// Use this component to record moving objects, IK animation, Ragdolls and any other object that may have movement applied to it.
    /// </summary>
    [DisallowMultipleComponent]
    [ReplaySerializer(typeof(ReplayTransformSerializer))]
    public class ReplayTransform : ReplayRecordableBehaviour
    {
        /// <summary>
        /// Replay flags used to specify how elements of the transform component should be recorded.
        /// </summary>
        [Flags]
        public enum ReplayTransformFlags
        {
            /// <summary>
            /// No data will be recorded or updated.
            /// </summary>
            None = 0,
            /// <summary>
            /// The X component of the transform element should be recorded.
            /// </summary>
            X = 1 << 0,
            /// <summary>
            /// The Y component of the transform element should be recorded.
            /// </summary>
            Y = 1 << 1,
            /// <summary>
            /// The Z component of the transform element should be recorded.
            /// </summary>
            Z = 1 << 2,
            /// <summary>
            /// The transform element should be recorded in local space. Note that scale will always be local.
            /// </summary>
            Local = 1 << 3,
            /// <summary>
            /// The transform element should be serialized using half precision. Not recommended for objects near the camera view.
            /// </summary>
            LowRes = 1 << 4,

            /// <summary>
            /// The transform element should be interpolated during playback.
            /// </summary>
            Interoplate = 1 << 8,

            /// <summary>
            /// The whole transform element should be recorded in world space and should be interpolated during playback.
            /// 
            /// </summary>
            XYZ_World = X | Y | Z | Interoplate,
            /// <summary>
            /// The whole transform element should be recorded in local space and should be interpolated during playback.
            /// </summary>
            XYZ_Local = X | Y | Z | Interoplate | Local,
        }

        // Private
        private static ReplayTransformSerializer sharedSerializer = new ReplayTransformSerializer();

#pragma warning disable 0414    // Complains about not being used but will be used at runtime (non-editor) for caching
        private ReplayTransformSerializer.ReplayTransformSerializeFlags storageFlags = 0;
#pragma warning restore 0414
        private ReplayTransformSerializer.ReplayTransformSerializeFlags updateFlags = 0;

        private Transform cachedTransform = null;
        private Vector3 targetPosition = Vector3.zero;
        private Vector3 lastPosition = Vector3.zero;
        private Quaternion targetRotation = Quaternion.identity;
        private Quaternion lastRotation = Quaternion.identity;
        private Vector3 targetScale = Vector3.one;
        private Vector3 lastScale = Vector3.one;

        // Public
        /// <summary>
        /// The transform position flags that specify which elements of the position are serialized and how that data is stored.
        /// </summary>
        [HideInInspector] // Displayed via custom inspector
        public ReplayTransformFlags positionFlags = ReplayTransformFlags.XYZ_World;
        /// <summary>
        /// The transform rotation flags that specify which elements of the rotation are serialized and how that data is stored.
        /// If full rotation is specified (XYZ) then the data will be stored as a quatenion for accuracy otherwise euler angles will be used.
        /// </summary>
        [HideInInspector] // Displayed via custom inspector
        public ReplayTransformFlags rotationFlags = ReplayTransformFlags.XYZ_World;
        /// <summary>
        /// The transform scale flags that specify which elements of the scale are serialized and how that data is stored.
        /// Note that Local and World flags has no effect for the scale element.
        /// </summary>
        [HideInInspector] // Displayed via custom inspector
        public ReplayTransformFlags scaleFlags = ReplayTransformFlags.None;

        // Properties
        private ReplayTransformSerializer.ReplayTransformSerializeFlags StorageFlags
        {
            get
            {
#if UNITY_EDITOR
                return storageFlags = GetDataFlags();
#else
                return storageFlags;
#endif
            }
        }

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            
            cachedTransform = transform;

            // Cache the data flags in game
            this.storageFlags = GetDataFlags();

            // Reset values
            OnReplayReset();
        }

        /// <summary>
        /// Called by the replay system when an instantiated replay prefab was spawned at the specified location.
        /// </summary>
        /// <param name="position">The position that the replay prefab instance was spawned at</param>
        /// <param name="rotation">The rotation that the replay prefab instance was spawned with</param>
        public override void OnReplaySpawned(Vector3 position, Quaternion rotation)
        {
			//cachedTransform.position = position;
			//cachedTransform.rotation = rotation;
			
            // Get the data flags
            ReplayTransformSerializer.ReplayTransformSerializeFlags flags = StorageFlags;


            // Get the correct new position
            if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalPos) != 0)
                lastPosition = targetPosition = cachedTransform.localPosition;
            else
                lastPosition = targetPosition = cachedTransform.position;

            // Get the correct new rotation
            if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalRot) != 0)
                lastRotation = targetRotation = cachedTransform.localRotation;
            else
                lastRotation = targetRotation = cachedTransform.rotation;

            // Get the scale
            lastScale = targetScale = cachedTransform.localScale;
        }

        /// <summary>
        /// Called by the replay system when the serializer component should reset any stored data.
        /// </summary>
        public override void OnReplayReset()
        {
            lastPosition = targetPosition;
            lastRotation = targetRotation;
            lastScale = targetScale;
        }

        /// <summary>
        /// Called by the replay system when playback is updated.
        /// </summary>
        /// <param name="replayTime">The current time value of the playback</param>
        public override void OnReplayUpdate(ReplayTime replayTime)
        {
            // Get the data flags
            ReplayTransformSerializer.ReplayTransformSerializeFlags flags = updateFlags;

            // Update position
            if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosX) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosY) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosZ) != 0)
                UpdatePosition(replayTime, flags, (positionFlags & ReplayTransformFlags.Interoplate) != 0);

            // Update rotation
            if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ) != 0)
                UpdateRotation(replayTime, flags, (rotationFlags & ReplayTransformFlags.Interoplate) != 0);

            // Update scale
            if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaX) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaY) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaZ) != 0)
                UpdateScale(replayTime, flags, (scaleFlags & ReplayTransformFlags.Interoplate) != 0);
        }

        /// <summary>
        /// Called by the replay system when the component should serialize its recoreded data.
        /// </summary>
        /// <param name="state">The replay state to store the data in</param>
        public override void OnReplaySerialize(ReplayState state)
        {
            // Update flags
            sharedSerializer.SerializeFlags = StorageFlags;

#if UNITY_EDITOR || ULTIMATEREPLAY_TRIAL
            if(Application.isPlaying == false || cachedTransform == null)
                cachedTransform = transform;
#else
            if(cachedTransform == null)
                cachedTransform = transform;
#endif

            // Update elements
            sharedSerializer.Position = ((StorageFlags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalPos) != 0) ? cachedTransform.localPosition : cachedTransform.position;
            sharedSerializer.Rotation = ((StorageFlags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalRot) != 0) ? cachedTransform.localRotation : cachedTransform.rotation;
            sharedSerializer.Scale = cachedTransform.localScale;

            // Run serializer
            sharedSerializer.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when the component should deserialize its stored data.
        /// </summary>
        /// <param name="state">The replay state to read the data from</param>
        public override void OnReplayDeserialize(ReplayState state)
        {
            // Update last values
            OnReplayReset();

            // Run serializer
            sharedSerializer.OnReplayDeserialize(state);

            // Get flags
            updateFlags = sharedSerializer.SerializeFlags;

            // Fetch transform elements
            targetPosition = sharedSerializer.Position;
            targetRotation = sharedSerializer.Rotation;
            targetScale = sharedSerializer.Scale;
        }

        private void UpdatePosition(ReplayTime time, ReplayTransformSerializer.ReplayTransformSerializeFlags flags, bool interpolate)
        {
            Vector3 updatePosition = targetPosition;

            // Interoplate the position
            if (interpolate == true)
                updatePosition = Vector3.Lerp(lastPosition, targetPosition, time.Delta);

            // Update the transform position
            if((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalPos) != 0)
            {
                // Get update values for selected axis
                float x = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosX) != 0) ? updatePosition.x : cachedTransform.localPosition.x;
                float y = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosY) != 0) ? updatePosition.y : cachedTransform.localPosition.y;
                float z = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosZ) != 0) ? updatePosition.z : cachedTransform.localPosition.z;

                // Use the local position
                cachedTransform.localPosition = new Vector3(x, y, z);
            }
            else
            {
                // Get update values for selected axis
                float x = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosX) != 0) ? updatePosition.x : cachedTransform.position.x;
                float y = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosY) != 0) ? updatePosition.y : cachedTransform.position.y;
                float z = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.PosZ) != 0) ? updatePosition.z : cachedTransform.position.z;

                // Use the world position
                cachedTransform.position = new Vector3(x, y, z);
            }
        }

        private void UpdateRotation(ReplayTime time, ReplayTransformSerializer.ReplayTransformSerializeFlags flags, bool interoplate)
        {
            Quaternion updateRotation = targetRotation;

            // Interoplate the rotation
            if (interoplate == true)
                updateRotation = Quaternion.Lerp(lastRotation, targetRotation, time.Delta);

            // Update the rotation position
            if((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalRot) != 0)
            {
                if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX) != 0 && (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY) != 0 && (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ) != 0)
                {
                    // Use the local rotation if all axis are recorded
                    cachedTransform.localRotation = updateRotation;
                }
                else
                {
                    Vector3 euler = updateRotation.eulerAngles;

                    // Get the individual axis rotation 
                    float x = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX) != 0) ? euler.x : cachedTransform.localEulerAngles.x;
                    float y = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY) != 0) ? euler.y : cachedTransform.localEulerAngles.y;
                    float z = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ) != 0) ? euler.z : cachedTransform.localEulerAngles.z;

                    // Use local rotation
                    cachedTransform.localRotation = Quaternion.Euler(x, y, z);
                }
            }
            else
            {
                if ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY) != 0 || (flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ) != 0)
                {
                    // Use the local rotation if all axis are recorded
                    transform.rotation = updateRotation;
                }
                else
                {
                    Vector3 euler = updateRotation.eulerAngles;

                    // Get the individual axis rotation 
                    float x = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX) != 0) ? updateRotation.x : transform.eulerAngles.x;
                    float y = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY) != 0) ? updateRotation.y : transform.eulerAngles.y;
                    float z = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ) != 0) ? updateRotation.z : transform.eulerAngles.z;

                    // Use local rotation
                    transform.rotation = Quaternion.Euler(x, y, z);
                }
            }
        }

        private void UpdateScale(ReplayTime time, ReplayTransformSerializer.ReplayTransformSerializeFlags flags, bool interoplate)
        {
            Vector3 updateScale = targetScale;

            // Interoplate the scale
            if (interoplate == true)
                updateScale = Vector3.Lerp(lastScale, targetScale, time.Delta);

            // Get update values for selected axis
            float x = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaX) != 0) ? updateScale.x : cachedTransform.localScale.x;
            float y = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaY) != 0) ? updateScale.y : cachedTransform.localScale.y;
            float z = ((flags & ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaZ) != 0) ? updateScale.z : cachedTransform.localScale.z;

            // Apply the scale
            cachedTransform.localScale = new Vector3(x, y, z);
        }

        private ReplayTransformSerializer.ReplayTransformSerializeFlags GetDataFlags()
        {
            ReplayTransformSerializer.ReplayTransformSerializeFlags dataFlags = 0;

            // Position flags
            if ((positionFlags & ReplayTransformFlags.X) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.PosX;
            if ((positionFlags & ReplayTransformFlags.Y) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.PosY;
            if ((positionFlags & ReplayTransformFlags.Z) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.PosZ;
            if ((positionFlags & ReplayTransformFlags.Local) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalPos;
            if ((positionFlags & ReplayTransformFlags.LowRes) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.LowPrecisionPos;

            // Rotation flags
            if ((rotationFlags & ReplayTransformFlags.X) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.RotX;
            if ((rotationFlags & ReplayTransformFlags.Y) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.RotY;
            if ((rotationFlags & ReplayTransformFlags.Z) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.RotZ;
            if ((rotationFlags & ReplayTransformFlags.Local) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.LocalRot;
            if ((rotationFlags & ReplayTransformFlags.LowRes) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.LowPrecisionRot;

            // Scale flags
            if ((scaleFlags & ReplayTransformFlags.X) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaX;
            if ((scaleFlags & ReplayTransformFlags.Y) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaY;
            if ((scaleFlags & ReplayTransformFlags.Z) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.ScaZ; // Skip LocalSca flag - Not used
            if ((scaleFlags & ReplayTransformFlags.LowRes) != 0) dataFlags |= ReplayTransformSerializer.ReplayTransformSerializeFlags.LowPrecisionSca;

            return dataFlags;
        }        
    }
}
