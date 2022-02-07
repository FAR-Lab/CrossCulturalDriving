using System;
using UltimateReplay.Core;
using UnityEngine;

namespace UltimateReplay.Serializers
{
    public sealed class ReplayTransformSerializer : IReplaySerialize
    {
        // Types
        [Flags]
        public enum ReplayTransformSerializeFlags : ushort
        {
            PosX = 1 << 0,
            PosY = 1 << 1,
            PosZ = 1 << 2,
            LocalPos = 1 << 3,
            LowPrecisionPos = 1 << 4,

            RotX = 1 << 5,
            RotY = 1 << 6,
            RotZ = 1 << 7,
            LocalRot = 1 << 8,
            LowPrecisionRot = 1 << 9,

            ScaX = 1 << 10,
            ScaY = 1 << 11,
            ScaZ = 1 << 12,
            LocalSca = 1 << 13,             // used for padding only
            LowPrecisionSca = 1 << 14,
        }

        // Private
        [ReplayTextSerialize("Serialize Flags")]
        private ReplayTransformSerializeFlags serializeFlags = 0;
        [ReplayTextSerialize("Position")]
        private Vector3 position = Vector3.zero;
        [ReplayTextSerialize("Rotation")]
        private Quaternion rotation = Quaternion.identity;
        [ReplayTextSerialize("Scale")]
        private Vector3 scale = Vector3.one;

        // Properties
        public Vector3 Position
        {
            get { return position; }
            set { position = value; }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
            set { rotation = value; }
        }

        public Vector3 Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public ReplayTransformSerializeFlags SerializeFlags
        {
            get { return serializeFlags; }
            set { serializeFlags = value; }
        }

        public bool IsWorldPosition
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LocalPos) == 0); }
            set
            {
                if (value == true)
                    serializeFlags &= ~ReplayTransformSerializeFlags.LocalPos;
                else
                    serializeFlags |= ReplayTransformSerializeFlags.LocalPos;
            }
        }

        public bool IsLocalPosition
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LocalPos) != 0); }
            set
            {
                if (value == true)
                    serializeFlags |= ReplayTransformSerializeFlags.LocalPos;
                else
                    serializeFlags &= ~ReplayTransformSerializeFlags.LocalPos;
            }
        }

        public bool IsFullPosition
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0 && (serializeFlags & ReplayTransformSerializeFlags.PosY) != 0 && (serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0); }
        }

        public bool IsLowPrecisionPosition
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0); }
            set
            {
                if (value == true)
                    serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionPos;
                else
                    serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionPos;
            }
        }

        public bool IsWorldRotation
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LocalRot) == 0); }
            set
            {
                if (value == true)
                    serializeFlags &= ~ReplayTransformSerializeFlags.LocalRot;
                else
                    serializeFlags |= ReplayTransformSerializeFlags.LocalRot;
            }
        }

        public bool IsLocalRotation
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LocalRot) != 0); }
            set
            {
                if (value == true)
                    serializeFlags |= ReplayTransformSerializeFlags.LocalRot;
                else
                    serializeFlags &= ~ReplayTransformSerializeFlags.LocalRot;
            }
        }

        public bool IsFullRotation
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0 && (serializeFlags & ReplayTransformSerializeFlags.RotY) != 0 && (serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0); }
        }

        public bool IsLowPrecisionRotation
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0); }
            set
            {
                if (value == true)
                    serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionRot;
                else
                    serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionRot;
            }
        }

        public bool IsFullScale
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0 && (serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0 & (serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0); }
        }

        public bool IsLowPrecisionScale
        {
            get { return ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0); }
            set
            {
                if (value == true)
                    serializeFlags |= ReplayTransformSerializeFlags.LowPrecisionSca;
                else
                    serializeFlags &= ~ReplayTransformSerializeFlags.LowPrecisionSca;
            }
        }

        // Constructor
        public ReplayTransformSerializer() { }

        public ReplayTransformSerializer(Vector3 position, Quaternion rotation, Vector3 scale, ReplayTransformSerializeFlags serializeFlags = 0)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;

            // Generate flags
            if(serializeFlags == 0)
            {
                serializeFlags |= ReplayTransformSerializeFlags.PosX | ReplayTransformSerializeFlags.PosY | ReplayTransformSerializeFlags.PosZ;
                serializeFlags |= ReplayTransformSerializeFlags.RotX | ReplayTransformSerializeFlags.RotY | ReplayTransformSerializeFlags.RotZ;
                serializeFlags |= ReplayTransformSerializeFlags.ScaX | ReplayTransformSerializeFlags.ScaY | ReplayTransformSerializeFlags.ScaZ;
            }

            this.serializeFlags = serializeFlags;
        }

        // Methods
        public void OnReplaySerialize(ReplayState state)
        {
            // Check for no data
            if (HasRecordableData() == false)
                return;

            // Write flags
            state.Write((ushort)serializeFlags);

            // Write position
            if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0)
            {
                // Write in low precision
                if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) state.WriteLowPrecision(position.x);
                if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) state.WriteLowPrecision(position.y);
                if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) state.WriteLowPrecision(position.z);
            }
            else
            {
                // Write in full precision
                if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) state.Write(position.x);
                if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) state.Write(position.y);
                if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) state.Write(position.z);
            }

            // Write rotation
            if(IsFullRotation == true)
            {                
                if((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Write in low precision
                    state.WriteLowPrecision(rotation);
                }
                else
                {
                    // Write in full perecision
                    state.Write(rotation);
                }
            }
            else
            {
                if((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Write in low precision
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) state.WriteLowPrecision(rotation.eulerAngles.x);
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) state.WriteLowPrecision(rotation.eulerAngles.y);
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) state.WriteLowPrecision(rotation.eulerAngles.z);
                }
                else
                {
                    // Write in full precision
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) state.Write(rotation.eulerAngles.x);
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) state.Write(rotation.eulerAngles.y);
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) state.Write(rotation.eulerAngles.z);
                }
            }
                        
            // Write scale
            if((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0)
            {
                // Write in low precision
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) state.WriteLowPrecision(scale.x);
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) state.WriteLowPrecision(scale.y);
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) state.WriteLowPrecision(scale.z);
            }
            else
            {
                // Write in full precision
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) state.Write(scale.x);
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) state.Write(scale.y);
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) state.Write(scale.z);
            }
        }

        public void OnReplayDeserialize(ReplayState state)
        {
            // Read flags
            serializeFlags = (ReplayTransformSerializeFlags)state.ReadUInt16();

            // Read position
            if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionPos) != 0)
            {
                // Read in low precision
                if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) position.x = state.ReadFloatLowPrecision();
                if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) position.y = state.ReadFloatLowPrecision();
                if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) position.z = state.ReadFloatLowPrecision();
            }
            else
            {
                // Read in full precision
                if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) position.x = state.ReadFloat();
                if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) position.y = state.ReadFloat();
                if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) position.z = state.ReadFloat();
            }

            // Read rotation
            if (IsFullRotation == true)
            {
                if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Read in low precision
                    rotation = state.ReadQuatLowPrecision();
                }
                else
                {
                    // Read in full perecision
                    rotation = state.ReadQuat();
                }
            }
            else
            {
                Vector3 eulerAngles = new Vector3();

                if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionRot) != 0)
                {
                    // Read in low precision
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) eulerAngles.x = state.ReadFloatLowPrecision();
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) eulerAngles.y = state.ReadFloatLowPrecision();
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) eulerAngles.z = state.ReadFloatLowPrecision();
                }
                else
                {
                    // Read in full precision
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) eulerAngles.x = state.ReadFloat();
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) eulerAngles.y = state.ReadFloat();
                    if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) eulerAngles.z = state.ReadFloat();
                }
                rotation = Quaternion.Euler(eulerAngles);
            }

            // Read scale
            if ((serializeFlags & ReplayTransformSerializeFlags.LowPrecisionSca) != 0)
            {
                // Read in low precision
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) scale.x = state.ReadFloatLowPrecision();
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) scale.y = state.ReadFloatLowPrecision();
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) scale.z = state.ReadFloatLowPrecision();
            }
            else
            {
                // Read in full precision
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) scale.x = state.ReadFloat();
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) scale.y = state.ReadFloat();
                if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) scale.z = state.ReadFloat();
            }
        }

        public void ApplyTo(Transform targetTransform)
        {
            // Apply all aspects of transform
            ApplyPositionTo(targetTransform);
            ApplyRotationTo(targetTransform);
            ApplyScaleTo(targetTransform);
        }

        public void ApplyPositionTo(Transform targetTransform)
        {
            Vector3 applyPos = targetTransform.position;
            bool local = IsLocalPosition;

            // Check for local
            if(local == true)
            {
                // Get original local pos
                applyPos = targetTransform.localPosition;
            }

            // Only apply specified aspects of position
            if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0) applyPos.x = position.x;
            if ((serializeFlags & ReplayTransformSerializeFlags.PosY) != 0) applyPos.y = position.y;
            if ((serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0) applyPos.z = position.z;

            if(local == true)
            {
                // Apply local position
                targetTransform.localPosition = applyPos;
            }
            else
            {
                // Apply world position
                targetTransform.position = applyPos;
            }
        }

        public void ApplyRotationTo(Transform targetTransform)
        {
            bool local = IsLocalRotation;

            // Check for euler or quaternion
            if (IsFullRotation == true)
            {
                if (local == true)
                {
                    // Apply local rotation
                    targetTransform.localRotation = rotation;
                }
                else
                {
                    // Apply world rotation
                    targetTransform.rotation = rotation;
                }
            }
            else
            {
                Vector3 eulerRot = targetTransform.eulerAngles;                

                if(local == true)
                {
                    // Get original local euler angles
                    eulerRot = targetTransform.localEulerAngles;
                }

                // Only apply specified aspects of rotation
                if ((serializeFlags & ReplayTransformSerializeFlags.RotX) != 0) eulerRot.x = rotation.eulerAngles.x;
                if ((serializeFlags & ReplayTransformSerializeFlags.RotY) != 0) eulerRot.y = rotation.eulerAngles.y;
                if ((serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0) eulerRot.z = rotation.eulerAngles.z;

                if(local == true)
                {
                    // Apply local rotation
                    targetTransform.localEulerAngles = eulerRot;
                }
                else
                {
                    // Apply world rotation
                    targetTransform.eulerAngles = eulerRot;
                }
            }
        }

        public void ApplyScaleTo(Transform targetTransform)
        {
            Vector3 applySca = targetTransform.localScale;

            // Only apply specified aspects of scale
            if ((serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0) applySca.x = scale.x;
            if ((serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0) applySca.y = scale.y;
            if ((serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0) applySca.z = scale.z;

            // Apply local scale
            targetTransform.localScale = applySca;
        }

        public bool HasRecordableData()
        {
            if ((serializeFlags & ReplayTransformSerializeFlags.PosX) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.PosY) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.PosZ) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.RotX) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.RotY) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.RotZ) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.ScaX) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.ScaY) != 0
                || (serializeFlags & ReplayTransformSerializeFlags.ScaZ) != 0)
                return true;

            return false;
        }

        public static ReplayTransformSerializer DeserializeReplayState(ReplayState state)
        {
            // Create a serializer
            ReplayTransformSerializer serializer = new ReplayTransformSerializer();

            // Try to deserialize
            serializer.OnReplayDeserialize(state);

            return serializer;
        }
    }
}
