using UnityEngine;

namespace UltimateReplay.Core.StatePreparer
{
    [ReplayComponentPreparer(typeof(Animator))]
    internal sealed class AnimatorPreparer : ComponentPreparer<Animator>
    {
        // Methods
        public override void PrepareForPlayback(Animator component, ReplayState additionalData)
        {
            // Write the component state
            additionalData.Write(component.enabled);

            // Disable the animator - it could interfere with playback
            component.enabled = false;
        }

        public override void PrepareForGameplay(Animator component, ReplayState additionalData)
        {
            // Read the component state
            bool initialState = additionalData.ReadBool();

            // Reset the animator state
            component.enabled = initialState;
        }
    }
}