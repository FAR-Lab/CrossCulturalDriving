using UnityEngine;

namespace UltimateReplay.Core.StatePreparer
{
    [ReplayComponentPreparer(typeof(Behaviour))]
    internal sealed class BehaviourPreparer : ComponentPreparer<Behaviour>
    {
        // Methods
        public override void PrepareForPlayback(Behaviour component, ReplayState additionalData)
        {
            // Store the initial value
            additionalData.Write(component.enabled);

            // Make the body kinematic to avoid movement by the physics engine
            component.enabled = false;
        }

        public override void PrepareForGameplay(Behaviour component, ReplayState additionalData)
        {
            // Read the default value
            bool initialState = additionalData.ReadBool();

            // Reset the kinematic state
            component.enabled = initialState;
        }
    }
}
