using UltimateReplay.Statistics;
using UnityEditor;

namespace UltimateReplay
{
    public static class ReplayStorageStats
    {
        // Methods
        public static void DisplayStorageStats(ReplayObject replayObject)
        {
            try
            {
                // Try to get size
                ReplayRecordableStatistics.ReplayObjectStatistics stats = ReplayRecordableStatistics.CalculateReplayRecordStorageUsage(replayObject);

                // Check for no components and empty size
                if (stats.evaluatedComponents== 0 && stats.byteSize == 0)
                    return;

                if (stats.byteSize == 0)
                {
                    EditorGUILayout.HelpBox("This replay object does not generate any data", MessageType.Info);
                }
                else if (stats.byteSize == -1)
                {
                    EditorGUILayout.HelpBox("Replay sample data will be displayed here during play mode", MessageType.Info);
                }
                else
                {
                    // Get the data size
                    decimal dataSize = ReplayStatisticsUtil.GetMemorySizeSmallestUnit(stats.byteSize);

                    // Get the data unit
                    string dataUnit = ReplayStatisticsUtil.GetMemoryUnitString(stats.byteSize);

                    // Extra message
                    string extraDetails = string.Empty;

                    if (stats.didSupressComponents == true)
                        extraDetails = ". (One or more observed components could not be evaluated at edit time)";

                    // Display field
                    EditorGUILayout.HelpBox(string.Format("This replay object generates '{0}' {1} per sample on average{2}", dataSize, dataUnit, extraDetails), MessageType.Info);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Refreshing replay statistics", MessageType.Warning);
            }
        }

        public static void DisplayStorageStats(params ReplayObject[] replayObject)
        {
            try
            {
                // Try to get size
                ReplayRecordableStatistics.ReplayObjectStatistics stats = ReplayRecordableStatistics.CalculateReplayRecordStorageUsage(replayObject);

                // Check for no components and empty size
                if (stats.evaluatedComponents == 0 && stats.byteSize == 0)
                    return;

                if (stats.byteSize == 0)
                {
                    EditorGUILayout.HelpBox("The selected replay objects do not generate any data", MessageType.Info);
                }
                else if (stats.byteSize == -1)
                {
                    EditorGUILayout.HelpBox("Replay sample data will be displayed here during play mode", MessageType.Info);
                }
                else
                {
                    // Get the data size
                    decimal dataSize = ReplayStatisticsUtil.GetMemorySizeSmallestUnit(stats.byteSize);

                    // Get the data unit
                    string dataUnit = ReplayStatisticsUtil.GetMemoryUnitString(stats.byteSize);

                    // Extra message
                    string extraDetails = string.Empty;

                    if (stats.didSupressComponents == true)
                        extraDetails = ". (One or more observed components could not be evaluated at edit time)";

                    // Display field
                    EditorGUILayout.HelpBox(string.Format("The selected replay objects generate '{0}' {1} per sample on average{2}", dataSize, dataUnit, extraDetails), MessageType.Info);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Refreshing replay statistics", MessageType.Warning);
            }
        }

        public static void DisplayStorageStats(ReplayRecordableBehaviour replayBehaviour)
        {
            try
            {
                // Try to get size
                int byteSize = ReplayRecordableStatistics.CalculateReplayRecordStorageUsage(replayBehaviour);

                if (byteSize == 0)
                {
                    EditorGUILayout.HelpBox("This replay component does not generate any data", MessageType.Info);
                }
                else if (byteSize == -1)
                {
                    EditorGUILayout.HelpBox("Replay sample data will be displayed here during play mode", MessageType.Info);
                }
                else
                {
                    // Get the data size
                    decimal dataSize = ReplayStatisticsUtil.GetMemorySizeSmallestUnit(byteSize);

                    // Get the data unit
                    string dataUnit = ReplayStatisticsUtil.GetMemoryUnitString(byteSize);

                    // Display field
                    EditorGUILayout.HelpBox(string.Format("This replay component generates '{0}' {1} per sample on average", dataSize, dataUnit), MessageType.Info);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Refreshing replay statistics", MessageType.Warning);
            }
        }

        public static void DisplayStorageStats(params ReplayRecordableBehaviour[] replayBehaviours)
        {
            try
            {
                // Try to get size
                int byteSize = ReplayRecordableStatistics.CalculateReplayRecordStorageUsage(replayBehaviours);

                if (byteSize == 0)
                {
                    EditorGUILayout.HelpBox("The selected replay components do not generate any data", MessageType.Info);
                }
                else if (byteSize == -1)
                {
                    EditorGUILayout.HelpBox("Replay sample data will be displayed here during play mode", MessageType.Info);
                }
                else
                {
                    // Get the data size
                    decimal dataSize = ReplayStatisticsUtil.GetMemorySizeSmallestUnit(byteSize);

                    // Get the data unit
                    string dataUnit = ReplayStatisticsUtil.GetMemoryUnitString(byteSize);

                    // Display field
                    EditorGUILayout.HelpBox(string.Format("The selected replay components generate '{0}' {1} per sample on average", dataSize, dataUnit), MessageType.Info);
                }
            }
            catch
            {
                EditorGUILayout.HelpBox("Refreshing replay statistics", MessageType.Warning);
            }
        }
    }
}
