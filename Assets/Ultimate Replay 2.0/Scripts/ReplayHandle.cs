using System;
using System.Collections.Generic;

namespace UltimateReplay
{
    /// <summary>
    /// Represents a handle to a specific replay record or playback operation.
    /// The handle is used as a unique identifier and is used by the replay manager to support mutliple simultaneous record and playback operations.
    /// </summary>
    public struct ReplayHandle : IDisposable
    {
        // Types
        /// <summary>
        /// The type of replay handle.
        /// </summary>
        public enum ReplayHandleType
        {
            /// <summary>
            /// Indicates that the replay handle is invalid.
            /// </summary>
            None = 0,
            /// <summary>
            /// The replay handle points to a replay record operation.
            /// </summary>
            Record,
            /// <summary>
            /// The replay handle points to a replay playback operation.
            /// </summary>
            Replay,
        }

        // Private 
        private static Random rand = new Random();
        private static HashSet<int> handleIDs = new HashSet<int>();
        private static HashSet<int> releasedHandleIDs = new HashSet<int>();
        private static int invalidHandleID = 0;

        private int handleID;
        private ReplayHandleType handleType;

        // Public
        /// <summary>
        /// Get a default invalid replay handle instance.
        /// </summary>
        public static readonly ReplayHandle invalid = new ReplayHandle();

        // Properties
        /// <summary>
        /// Returns true if the replay handle has been disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return handleID == invalidHandleID; }
        }

        internal ReplayHandleType ReplayType
        {
            get
            {
                CheckDisposed();
                return handleType;
            }
        }

        // Constructor
        internal ReplayHandle(ReplayHandleType type)
        {
            this.handleID = UniqueHandleID();
            this.handleType = type;
        }

        // Methods
        /// <summary>
        /// Dispose the replay handle.
        /// Take care when disposing replay handles. Disposing the handle before the corrospoding replay operation has finished will make it impossible to access that operation.
        /// Note that dispose is automatially called in replay methods that take a replay handle instance as a ref parameter.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed == true)
                return;

            this.handleID = invalidHandleID;
            this.handleType = ReplayHandleType.None;
        }

        /// <summary>
        /// Override implementation of Equals.
        /// </summary>
        /// <param name="obj">The object to compare</param>
        /// <returns>True if the objects are equal or false if not</returns>
        public override bool Equals(object obj)
        {
            try
            {
                ReplayHandle handle = (ReplayHandle)obj;
                return handleID == handle.handleID;
            }
            catch { }

            return base.Equals(obj);
        }

        /// <summary>
        /// Override implementaton of GetHasCode.
        /// </summary>
        /// <returns>The hash code for the replay handle</returns>
        public override int GetHashCode()
        {
            return handleID.GetHashCode();
        }

        private void CheckDisposed()
        {
            if (this.handleID == invalidHandleID)
                throw new ObjectDisposedException("The replay handle has been disposed");
        }

        private static int UniqueHandleID()
        {
            int value = invalidHandleID;

            while(value == invalidHandleID || handleIDs.Contains(value) == true || releasedHandleIDs.Contains(value) == true)
            {
                value = rand.Next();
            }

            return value;
        }

        internal static void Release(ReplayHandle handle)
        {
            // Remove from used set
            if (handleIDs.Contains(handle.handleID) == true)
                handleIDs.Remove(handle.handleID);

            // Add to released set
            releasedHandleIDs.Add(handle.handleID);
        }
    }
}
