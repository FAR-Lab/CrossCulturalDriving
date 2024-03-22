using UnityEngine;

namespace Mocopi.Receiver
{
    /// <summary>
    /// Abstract class for applying mocopi bone information to avatars
    /// </summary>
    public abstract class MocopiAvatarBase : MonoBehaviour
    {
        #region --Methods--
        /// <summary>
        /// Initialize avatar bone information
        /// </summary>
        /// <param name="boneIds">mocopi Avatar bone id list</param>
        /// <param name="parentJointIds">List of IDs of parent bones for each bone</param>
        /// <param name="rotationsX">Rotation angle of each bone in initial posture</param>
        /// <param name="rotationsY">Rotation angle of each bone in initial posture</param>
        /// <param name="rotationsZ">Rotation angle of each bone in initial posture</param>
        /// <param name="rotationsW">Rotation angle of each bone in initial posture</param>
        /// <param name="positionsX">Position of each bone in initial pose</param>
        /// <param name="positionsY">Position of each bone in initial pose</param>
        /// <param name="positionsZ">Position of each bone in initial pose</param>
        public virtual void InitializeSkeleton(
            int[] boneIds, int[] parentBoneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        )
        {
        }

        /// <summary>
        /// Update avatar bone information
        /// </summary>
        /// <param name="frameId">Frame Id</param>
        /// <param name="timestamp">Timestamp</param>
        /// <param name="unixTime">Unix time when sensor sent data</param>
        /// <param name="boneIds">mocopi Avatar bone id list</param>
        /// <param name="rotationsX">Rotation angle of each bone</param>
        /// <param name="rotationsY">Rotation angle of each bone</param>
        /// <param name="rotationsZ">Rotation angle of each bone</param>
        /// <param name="rotationsW">Rotation angle of each bone</param>
        /// <param name="positionsX">Position of each bone</param>
        /// <param name="positionsY">Position of each bone</param>
        /// <param name="positionsZ">Position of each bone</param>
        public virtual void UpdateSkeleton(
            int frameId, float timestamp, double unixTime,
            int[] boneIds,
            float[] rotationsX, float[] rotationsY, float[] rotationsZ, float[] rotationsW,
            float[] positionsX, float[] positionsY, float[] positionsZ
        )
        {
        }
        #endregion --Methods--
    }
}