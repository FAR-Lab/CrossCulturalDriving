using UnityEngine;

namespace UltimateReplay.Core.StatePreparer
{
    [ReplayComponentPreparer(typeof(Collider))]
    internal sealed class ColliderPreparer : ComponentPreparer<Collider>
    {
        // Methods
        public override void PrepareForPlayback(Collider component, ReplayState additionalData)
        {
            // Store the initial value
            additionalData.Write(component.enabled);

            // Make the body kinematic to avoid movement by the physics engine
            component.enabled = false;
        }

        public override void PrepareForGameplay(Collider component, ReplayState additionalData)
        {
            // Read the default value
            bool initialState = additionalData.ReadBool();

            // Reset the kinematic state
            component.enabled = initialState;
        }
    }

    [ReplayComponentPreparer(typeof(Collider2D))]
    internal sealed class Collider2DPreparer : ComponentPreparer<Collider2D>
    {
        // Methods
        public override void PrepareForPlayback(Collider2D component, ReplayState additionalData)
        {
            // Store the initial value
            additionalData.Write(component.enabled);

            // Make the body kinematic to avoid movement by the physics engine
            component.enabled = false;
        }

        public override void PrepareForGameplay(Collider2D component, ReplayState additionalData)
        {
            // Read the default value
            bool initialState = additionalData.ReadBool();

            // Reset the kinematic state
            component.enabled = initialState;
        }
    }
}
