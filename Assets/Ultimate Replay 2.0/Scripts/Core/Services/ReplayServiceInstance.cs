using System;
using UltimateReplay.Storage;

namespace UltimateReplay.Core.Services
{
    internal abstract class ReplayServiceInstance : IDisposable
    {
        // Types
        public enum ReplayServiceState
        {
            Active,
            Paused,
        }

        // Protected
        protected ReplayHandle handle = ReplayHandle.invalid;
        protected ReplayServiceState state = ReplayServiceState.Active;
        protected ReplayScene scene = null;
        protected ReplayStorageTarget target = null;

        // Properties
        public ReplayHandle Handle
        {
            get { return handle; }
        }

        public ReplayServiceState State
        {
            get { return state; }
            set { state = value; }
        }

        public ReplayScene Scene
        {
            get { return scene; }
        }

        public ReplayStorageTarget Target
        {
            get { return target; }
        }

        public abstract UltimateReplay.UpdateMethod UpdateMethod { get; }

        // Methods
        public abstract void ReplayUpdate(float deltaTime);

        protected void Initialize(ReplayHandle handle, ReplayServiceState state, ReplayScene scene, ReplayStorageTarget target)
        {
            this.handle = handle;
            this.state = state;
            this.scene = scene;
            this.target = target;            
        }

        public virtual void Dispose()
        {
            this.handle = ReplayHandle.invalid;
            this.scene = null;
            this.target = null;
        }

        public bool IsUpdateable(UltimateReplay.UpdateMethod updateMethod)
        {
            if (state == ReplayServiceState.Paused || updateMethod != UpdateMethod)
                return false;

            return true;
        }

        public bool IsUpdateable()
        {
            if (state == ReplayServiceState.Paused)
                return false;

            return true;
        }
    }
}
