using UnityEngine;

namespace UltimateReplay.Core.StatePreparer
{
    [ReplayComponentPreparer(typeof(Rigidbody))]
    internal sealed class RigidBodyPreparer : ComponentPreparer<Rigidbody>
    {
        // Methods
        public override void PrepareForPlayback(Rigidbody component, ReplayState additionalData)
        {
            // Store the initial value
            additionalData.Write(component.isKinematic);

            // Make the body kinematic to avoid movement by the physics engine
            component.isKinematic = true;
            
        }

        public override void PrepareForGameplay(Rigidbody component, ReplayState additionalData)
        {
            // Read the default value
            bool initialState =  additionalData.ReadBool();

            // Reset the kinematic state
            component.isKinematic = initialState;
        }
    }

    [ReplayComponentPreparer(typeof(Rigidbody2D))]
    internal sealed class RigidBody2DPreparer : ComponentPreparer<Rigidbody2D>
    {
        // Methods
        public override void PrepareForPlayback(Rigidbody2D component, ReplayState additionalData)
        {
            // Store the initial value
            additionalData.Write(component.isKinematic);
            additionalData.Write(component.simulated);

            // Make the body kinematic to avoid movement by the physics engine
            component.isKinematic = true;
            component.simulated = false;
        }

        public override void PrepareForGameplay(Rigidbody2D component, ReplayState additionalData)
        {
            // Read the default value
            bool initialState = additionalData.ReadBool();
            bool initialSimulated = additionalData.ReadBool();

            // Reset the kinematic state and simulated
            component.isKinematic = initialState;
            component.simulated = initialSimulated;
        }
    }
}