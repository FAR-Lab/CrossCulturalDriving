using UnityEngine;

namespace UltimateReplay.Core
{
    internal struct ReplayTimer
    {
        // Private
        private float startTime;
        private float timer;

        // Properties
        public float ElapsedSeconds
        {
            get { return timer - startTime; }
        }

        // Methods
        public void Tick(float deltaTime)
        {
            timer += deltaTime;
        }

        public bool HasElapsed(float time)
        {
            // Check if enough time has passed
            if(timer >= (startTime + time))
            {
                Reset();
                return true;
            }
            return false;
        }
        
        public void Reset()
        {
            // Reset our timer
            startTime = timer;
        }
    }
}
