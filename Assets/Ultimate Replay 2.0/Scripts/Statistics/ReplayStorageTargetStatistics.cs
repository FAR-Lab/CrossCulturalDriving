using System;
using System.Collections.Generic;
using UltimateReplay.Storage;

namespace UltimateReplay.Statistics
{
    public static class ReplayStorageTargetStatistics
    {
        // Private
        private static List<WeakReference> storageTargets = new List<WeakReference>();
        private static Stack<int> deadStorageTargets = new Stack<int>();

        // Methods
        internal static void AddStorageTarget(ReplayStorageTarget target)
        {
            // Register storage target
            storageTargets.Add(new WeakReference(target));
        }

        public static int CalculateReplayMemoryUsage()
        {
            // Remove all dead references
            RemoveDeadReferences();

            int size = 0;

            foreach(WeakReference reference in storageTargets)
            {
                // Get target
                ReplayStorageTarget target = reference.Target as ReplayStorageTarget;

                // Add storage size
                size += target.MemorySize;
            }

            return size;
        }

        

        private static void RemoveDeadReferences()
        {
            // Check for dead items
            for(int i = 0; i < storageTargets.Count; i++)
            {
                if(storageTargets[i].IsAlive == false)
                {
                    deadStorageTargets.Push(i);
                }
            }

            // Remove dead items
            while (deadStorageTargets.Count > 0)
                storageTargets.RemoveAt(deadStorageTargets.Pop());
        }
    }
}
