using System;
using System.Collections.Generic;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Used to initialize existing and reused instances in conjunction with the <see cref="ReplayInstancePool{T}"/>.
    /// </summary>
    public interface IReplayReusable
    {
        // Methods
        /// <summary>
        /// Called when an existing instance is about to be returned from the pool. 
        /// This method should reset any field members to default or safe values.
        /// </summary>
        void Initialize();
    }

    /// <summary>
    /// An instance pool used to recycle managed non-Unity objects.
    /// </summary>
    /// <typeparam name="T">The type of object to manage</typeparam>
    public sealed class ReplayInstancePool<T>
    {
        // Private
        private Stack<T> waitingInstances = new Stack<T>();
        private Func<T> newT = null;

        private object sync = new object();

        // Constructor
        internal ReplayInstancePool(Func<T> newT)
        {
            this.newT = newT;
        }

        // Methods
        /// <summary>
        /// Get an existing resycled instance or create a new instance if required.
        /// Instances which implement the <see cref="IReplayReusable"/> interface will have the <see cref="IReplayReusable.Initialize"/> method called if a recycled instance is used.
        /// </summary>
        /// <returns>An instance of T</returns>
        public T GetReusable()
        {
            lock (sync)
            {
                if (waitingInstances.Count > 0)
                {
                    // Get the instance
                    T instance = waitingInstances.Pop();

                    // Call initialize to reset the instance
                    if (instance is IReplayReusable)
                    {
                        ((IReplayReusable)instance).Initialize();
                    }

                    return instance;
                }
            }

            // Create new instance
            return newT();
        }

        /// <summary>
        /// Return an exsting instance to the pool which is no longer required.
        /// </summary>
        /// <param name="reusableInstance">The T instance to return to the pool</param>
        public void PushReusable(T reusableInstance)
        {
            lock (sync)
            {
                // Push to waiting set
                if (reusableInstance != null && waitingInstances.Contains(reusableInstance) == false)
                {
                    waitingInstances.Push(reusableInstance);
                }
            }
        }
    }
}
