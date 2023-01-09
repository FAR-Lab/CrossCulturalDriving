using System;

namespace UltimateReplay.Statistics
{
    public static class ReplayRecordableStatistics
    {
        // Types
        public struct ReplayObjectStatistics
        {
            // Public
            public int byteSize;
            public int evaluatedComponents;
            public int supressedComponents;
            public bool didSupressComponents;
        }

        // Private
        private static bool supressedStatistics = false;

        // Methods
        public static ReplayObjectStatistics CalculateReplayRecordStorageUsage(ReplayObject replayObject)
        {
            // Check for error
            if (replayObject == null)
                throw new ArgumentNullException("replayObject");

            // Get a replay state for evaluation purposes
            ReplayState tempState = ReplayState.pool.GetReusable();

            ReplayObjectStatistics stats = new ReplayObjectStatistics();

            // Serialize the object
            replayObject.OnReplaySerialize(tempState);

            // CHeck for supressed
            if(supressedStatistics == true)
            {
                supressedStatistics = false;
                stats.supressedComponents++;
                //return -1;
            }

            // Set suppressed flag
            stats.didSupressComponents = stats.supressedComponents > 0;
            stats.evaluatedComponents = replayObject.ObservedComponents.Count;

            if (stats.evaluatedComponents > 0 && stats.supressedComponents == stats.evaluatedComponents)
            {
                // Supressed all components
                stats.byteSize = -1;
            }
            else
            {
                // Get the size
                stats.byteSize = tempState.Size;
            }

            // Release the state
            tempState.Dispose();

            return stats;
        }

        public static ReplayObjectStatistics CalculateReplayRecordStorageUsage(params ReplayObject[] replayObjects)
        {
            // Check for error
            if (replayObjects == null)
                throw new ArgumentNullException("replayObjects");

            // Get a replay state for evaluation purposes
            ReplayState tempState = ReplayState.pool.GetReusable();

            ReplayObjectStatistics stats = new ReplayObjectStatistics();
            int componentCount = 0;

            // Process all
            foreach (ReplayObject replayObject in replayObjects)
            {
                // Check for null
                if (replayObject == null)
                    continue;

                // Serialize the behaviour
                replayObject.OnReplaySerialize(tempState);

                // Check if statistics have been supressed
                if (supressedStatistics == true)
                {
                    supressedStatistics = false;
                    stats.supressedComponents++;
                    //continue;
                }

                componentCount += replayObject.ObservedComponents.Count;
            }


            // Set suppressed flag
            stats.didSupressComponents = stats.supressedComponents > 0;
            stats.evaluatedComponents = componentCount;

            if (stats.evaluatedComponents > 0 && stats.supressedComponents == stats.evaluatedComponents)
            {
                // Supressed all components
                stats.byteSize = -1;
            }
            else
            {
                // Get the size
                stats.byteSize = tempState.Size;
            }

            // Release the state
            tempState.Dispose();

            return stats;
        }

        public static int CalculateReplayRecordStorageUsage(ReplayRecordableBehaviour replayBehaviour)
        {
            // Check for error
            if (replayBehaviour == null)
                throw new ArgumentNullException("replayBehaviour");

            // Get a replay state for evaluation purposes
            ReplayState tempState = ReplayState.pool.GetReusable();

            // Serialize the behaviour
            replayBehaviour.OnReplaySerialize(tempState);

            // Check if statistics have been supressed
            if(supressedStatistics == true)
            {
                supressedStatistics = false;
                return -1;
            }

            // Get the size
            int storageSize = tempState.Size;

            // Release the state
            tempState.Dispose();

            return storageSize;
        }

        public static int CalculateReplayRecordStorageUsage(params ReplayRecordableBehaviour[] replayBehaviours)
        {
            // Check for error
            if (replayBehaviours == null)
                throw new ArgumentNullException("replayBehaviours");

            // Get a replay state for evaluation purposes
            ReplayState tempState = ReplayState.pool.GetReusable();

            // Process all
            foreach (ReplayRecordableBehaviour replayBehaviour in replayBehaviours)
            {
                // Check for null
                if (replayBehaviour == null)
                    continue;

                // Serialize the behaviour
                replayBehaviour.OnReplaySerialize(tempState);

                // Check if statistics have been supressed
                if (supressedStatistics == true)
                {
                    supressedStatistics = false;
                    continue;
                }
            }

            // Get the size
            int storageSize = tempState.Size;

            // Release the state
            tempState.Dispose();

            return storageSize;
        }

        public static void SupressStatisticsDuringEditMode()
        {
            supressedStatistics = true;
        }
    }
}
