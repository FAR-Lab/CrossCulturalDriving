using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Util class that stores accuracy errors that have occured during playback.
    /// Playback errors are generated when one or more objects in the scene could not be restored to their previously recorded state.
    /// </summary>
	public class ReplayPlaybackAccuracyReporter
	{
		// Types
        /// <summary>
        /// The playback accuracy error type.
        /// </summary>
		public enum PlaybackAccuracyError
		{
            /// <summary>
            /// Cannot destroy the target game object because it is not a registered prefab instance.
            /// </summary>
			DestroyNotARegisteredPrefab,
            /// <summary>
            /// The target object could not be found in the scene and there is no corrosponding prefab registered with the replay system to use for dynamic instantiate.
            /// </summary>
			InstantiateMissingObjectAndNotPrefab,
            /// <summary>
            /// Cannot instantiate the target game object on demand because there is no corrosponding prefab registered with the replay system.
            /// </summary>
			InstantiatePrefabNotFound,
		}

		// Private
		private static Dictionary<PlaybackAccuracyError, List<ReplayIdentity>> reportedErrors = new Dictionary<PlaybackAccuracyError, List<ReplayIdentity>>();

		// Methods
        /// <summary>
        /// Report an error in playback accuracy.
        /// </summary>
        /// <param name="replayIdentity">The identity of the target object that generated the error</param>
        /// <param name="errorType">The type of playback accuracy error</param>
		public static void RecordPlaybackAccuracyError(ReplayIdentity replayIdentity, PlaybackAccuracyError errorType)
		{
			// Report the error
			string errorMsg = GetPlaybackAccuracyErrorMessage(replayIdentity, errorType);

			if (errorMsg != null)
			{
				// Store the error reported info
				if (reportedErrors.ContainsKey(errorType) == false)
					reportedErrors.Add(errorType, new List<ReplayIdentity>());

				// Check if the error has been reported
				if (reportedErrors[errorType].Contains(replayIdentity) == false)
				{
					// Add to used errors
					reportedErrors[errorType].Add(replayIdentity);
				}
			}
		}

        /// <summary>
        /// Report all playback accuracy errors to the Unity console.
        /// </summary>
		public static void ReportAllPlaybackAccuracyErrors()
		{
			int totalCount = 0;

			// Find the total number of playback errors
			foreach(KeyValuePair<PlaybackAccuracyError, List<ReplayIdentity>> item in reportedErrors)
				totalCount += item.Value.Count;

			// Check for errors
			if(totalCount > 0)
			{
				Debug.LogWarningFormat("Replay playack accurary could not be achieved. There were '{0}' playback errors", totalCount);

				StringBuilder builder = new StringBuilder();

				foreach(KeyValuePair<PlaybackAccuracyError, List<ReplayIdentity>> item in reportedErrors)
				{
					if (item.Value.Count == 1)
					{
						string errorMessage = GetPlaybackAccuracyErrorMessage(item.Value[0], item.Key);

						if (errorMessage != null)
							Debug.LogWarning(errorMessage);
					}
					else
					{
						string errorMessage = GetPlaybackAccuracyErrorMessage(item.Key, item.Value.Count);

						if(errorMessage != null)
						{
							builder.Append(errorMessage);
							builder.Append(" (");

							for(int i = 0; i < item.Value.Count; i++)
							{
								builder.Append(item.Value[i]);

								if (i < item.Value.Count - 1)
									builder.Append(", ");
								else
									builder.Append(")");
							}

							// Log playback error
							Debug.LogWarning(builder.ToString());

							// Reset builder
							builder.Length = 0;
						}
					}
				}
			}

			// Reset for next frame
			reportedErrors.Clear();
		}

        /// <summary>
        /// Get the error message for the specified playback accuracy error.
        /// </summary>
        /// <param name="errorType">The type of error to return the message for</param>
        /// <param name="errorCount">The number of errors that were generated</param>
        /// <returns>A string message describing the playback accuracy error</returns>
		public static string GetPlaybackAccuracyErrorMessage(PlaybackAccuracyError errorType, int errorCount)
		{
			switch (errorType)
			{
				case PlaybackAccuracyError.DestroyNotARegisteredPrefab:
					return string.Format("Replay playback accuracy error: '{0}' replay objects should not exist at the current time but cannot be destroyed as they are not registered as replay prefabs. Consider registering the following objects as replay prefabs so that Instantiate/Destroy are supported", errorCount);

				case PlaybackAccuracyError.InstantiateMissingObjectAndNotPrefab:
					return string.Format("Replay playback accuracy error: Failed to recreate '{0}' replay objects. The objects could not be found within the current scene and are not registered as replay prefabs. You may need to reload the scene before playback to ensure that all recorded objects are present.", errorCount);

				case PlaybackAccuracyError.InstantiatePrefabNotFound:
					return string.Format("Replay playback accuracy error: Failed to recreate '{0}' replay prefabs. No such prefabs are registered.", errorCount);
			}
			return null;
		}

        /// <summary>
        /// Get the playback accuracy error for the specified target object identity.
        /// </summary>
        /// <param name="replayIdentity">The identity of the target object</param>
        /// <param name="errorType">The type of playback accuracy error you want to generate</param>
        /// <returns>A string message describing the playback accuracy error for the specified target object</returns>
		public static string GetPlaybackAccuracyErrorMessage(ReplayIdentity replayIdentity, PlaybackAccuracyError errorType)
		{
			switch (errorType)
			{
				case PlaybackAccuracyError.DestroyNotARegisteredPrefab:
					return string.Format("Replay playback accuracy error: The replay object with identity '{0}' should not exist at the current time but cannot be destroyed as it is not a registered prefab. Consider registering the object as a prefab so that Instantiate/Destroy are supported", replayIdentity);

				case PlaybackAccuracyError.InstantiateMissingObjectAndNotPrefab:
					return string.Format("Replay playback accuracy error: Failed to recreate replay object with identity '{0}'. The object could not be found within the current scene and it is not a registered replay prefab. You may need to reload the scene before playback to ensure that all recorded objects are present.", replayIdentity);

				case PlaybackAccuracyError.InstantiatePrefabNotFound:
					return string.Format("Replay playback accuracy error: Failed to recreate replay prefab '{0}'. No such prefab is registered.", replayIdentity);
			}
			return null;
		}
	}
}
