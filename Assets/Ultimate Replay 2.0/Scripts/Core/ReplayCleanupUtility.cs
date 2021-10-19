using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.Core
{
    internal static class ReplayCleanupUtility
    {
        // Private
        private static HashSet<IDisposable> unreleasedResources = new HashSet<IDisposable>();
        private static bool lockCollection = false;

        // Methods
        public static void RegisterUnreleasedResource(IDisposable resource)
        {
            if (unreleasedResources.Contains(resource) == false && lockCollection == false)
                unreleasedResources.Add(resource);
        }

        public static void UnregisterUnreleasedResource(IDisposable resource)
        {
            if (unreleasedResources.Contains(resource) == true && lockCollection == false)
                unreleasedResources.Remove(resource);
        }

        public static void CleanupUnreleasedResources()
        {
            // Prevent collection from being modified during iteration
            lockCollection = true;

            foreach(IDisposable resource in unreleasedResources)
            {
                try
                {
                    Debug.LogWarningFormat("Cleaning up unreleased resource: {0}. You should manually release this resource by calling 'Dispose' before the game exits", resource);
                    resource.Dispose();
                }
                catch(Exception e)
                {
                    Debug.LogException(e);
                }
            }

            unreleasedResources.Clear();
            lockCollection = false;
        }
    }
}
