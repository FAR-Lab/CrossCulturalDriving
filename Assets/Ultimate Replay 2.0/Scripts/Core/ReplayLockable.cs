using System;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Base class for any replay system element that should only allow single access. 
    /// Allows the resource to only be used in a single operation at any time.
    /// </summary>
    public abstract class ReplayLockable
    {
        // Types
        /// <summary>
        /// Contains information about the lock state of the object.
        /// </summary>
        public struct LockableContext
        {
            // Public
            /// <summary>
            /// The object that is using this resource.
            /// </summary>
            public object lockOwner;
            /// <summary>
            /// The <see cref="ReplayHandle"/> that is using this resource.
            /// </summary>
            public ReplayHandle lockHandle;

            // Properties
            /// <summary>
            /// Returns a value indicating whether this resource is locked. 
            /// </summary>
            public bool IsLocked
            {
                get
                {
                    if (lockOwner == null && lockHandle.Equals(ReplayHandle.invalid) == true)
                        return false;

                    return true;
                }
            }
        }

        // Private
        private LockableContext context = new LockableContext();

        // Properties
        /// <summary>
        /// Get the lock context for the resource which provides information such as the lock owner.
        /// </summary>
        public LockableContext LockContext
        {
            get { return context; }
        }

        // Methods
        /// <summary>
        /// Attempt to lock this resource.
        /// </summary>
        /// <param name="lockOwner">The object that will be using the resource</param>
        /// <param name="lockHandle">The identity of the owning process</param>
        public void Lock(object lockOwner, ReplayHandle lockHandle)
        {
            if (context.IsLocked == true)
                throw new AccessViolationException(GetType() + " is in use by another replay process");

            this.context = new LockableContext
            {
                lockOwner = lockOwner,
                lockHandle = lockHandle,
            };
        }

        /// <summary>
        /// Release the resource so that it can be used by another operation.
        /// </summary>
        /// <param name="lockOwner">The object that initially locked the resource</param>
        /// <param name="lockHandle">The identity of the owning process</param>
        public void Unlock(object lockOwner, ReplayHandle lockHandle)
        {
            // Make sure the owner is calling unlock
            if (context.lockOwner == lockOwner && context.lockHandle.Equals(lockHandle) == true)
            {
                this.context = new LockableContext();
            }
        }
    }
}
