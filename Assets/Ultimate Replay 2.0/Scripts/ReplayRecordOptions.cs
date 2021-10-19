using System;
using UnityEngine;

namespace UltimateReplay
{
    /// <summary>
    /// A number of options that can be used to control the record behaviour.
    /// </summary>
    [Serializable]
    public sealed class ReplayRecordOptions
    {
        // Private
        [Range(minRecordFPS, maxRecordFPS)]
        [SerializeField]
        private int recordFPS = 16;

        [SerializeField]
        private UltimateReplay.UpdateMethod recordUpdateMethod = UltimateReplay.UpdateMethod.Update;

        // Public
        /// <summary>
        /// The minimum allowable record frame rate.
        /// </summary>
        public const int minRecordFPS = 1;
        /// <summary>
        /// The maximum allowable record frame rate.
        /// </summary>
        public const int maxRecordFPS = 48;

        // Properties
        /// <summary>
        /// The target record frame rate.
        /// Higher frame rates will result in more storage consumption but better replay accuracy.
        /// </summary>
        public int RecordFPS
        {
            get { return recordFPS; }
            set { recordFPS = value; }
        }

        /// <summary>
        /// The update method used to update the record operation.
        /// Used for compatibility with other systems that update objects in other update methods such as LateUpdate.
        /// </summary>
        public UltimateReplay.UpdateMethod RecordUpdateMethod
        {
            get { return recordUpdateMethod; }
            set { recordUpdateMethod = value; }
        }

        // Methods
    }
}
