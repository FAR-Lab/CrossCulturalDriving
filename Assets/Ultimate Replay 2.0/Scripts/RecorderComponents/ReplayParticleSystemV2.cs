using System;
using UnityEngine;

namespace UltimateReplay
{
    public class ReplayParticleSystemV2 : ReplayRecordableBehaviour
    {
        // Private
        private bool particleSystemPlayingOnStart = false;

        // Public
        public ParticleSystem observedParticleSystem = null;

        // Methods
        public void Start()
        {
            if (observedParticleSystem == null)
                Debug.LogWarningFormat("Replay particle system '{0}' will not record or replay because the observed particle system has not been assigned", this);
        }

        public override void OnReplayStart()
        {
            particleSystemPlayingOnStart = observedParticleSystem.isPlaying;
        }

        public override void OnReplayEnd()
        {
            // Reset particles
            observedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Begin playing again
            if (particleSystemPlayingOnStart == true)
                observedParticleSystem.Play(true);
        }

        public override void OnReplaySerialize(ReplayState state)
        {
            // Check for no component
            if (observedParticleSystem == null)
                return;

            // Serialize values
            state.Write(observedParticleSystem.time);
            state.Write(observedParticleSystem.isPlaying);
        }

        public override void OnReplayDeserialize(ReplayState state)
        {
            float time = state.ReadFloat();
            bool playing = state.ReadBool();

            // Check for playing
            if(playing == true)
            {
                // Start playing if not already
                if(observedParticleSystem.isPlaying == false)
                {
                    observedParticleSystem.Simulate(time, true, true);
                    observedParticleSystem.Play();
                }
            }
            else
            {
                // Stop playing if not running
                if(observedParticleSystem.isPlaying == true)
                {
                    observedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }

            // Check for large delta
            if(Mathf.Abs(time) - observedParticleSystem.time > .2f)
            {
                observedParticleSystem.Simulate(time, true, true);
                observedParticleSystem.Play();
            }
        }
    }
}
