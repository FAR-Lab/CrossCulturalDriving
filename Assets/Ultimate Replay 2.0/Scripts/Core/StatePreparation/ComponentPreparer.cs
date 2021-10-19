using UnityEngine;
using System.Collections.Generic;

namespace UltimateReplay.Core.StatePreparer
{
    internal abstract class ComponentPreparer
    {
        // Private
        private ReplayComponentPreparerAttribute attribute = null;

        // Properties
        public bool enabled = true;

        // Properties
        internal ReplayComponentPreparerAttribute Attribute
        {
            get { return attribute; }
            set { attribute = value; }
        }

        // Methods
        internal abstract void InvokePrepareForPlayback(Component component);

        internal abstract void InvokePrepareForGameplay(Component component);
    }

    internal abstract class ComponentPreparer<T> : ComponentPreparer where T : Component
    {
        // Private
        private Dictionary<int, ReplayState> componentData = new Dictionary<int, ReplayState>();
        
        // Methods
        public abstract void PrepareForPlayback(T component, ReplayState additionalData);

        public abstract void PrepareForGameplay(T component, ReplayState additionalData);

        internal override void InvokePrepareForPlayback(Component component)
        {            
            // Check for correct type
            if ((component is T) == false)
                return;

            // Create a state to hold the data
            ReplayState state = ReplayState.pool.GetReusable();

            // Call the method
            PrepareForPlayback(component as T, state);

            // Check for any state information
            if(state.Size > 0)
            {
                // Get the component hash
                int hash = component.GetInstanceID();

                // Check if it already exists
                if(componentData.ContainsKey(hash) == true)
                {
                    // Update the initial state
                    componentData[hash] = state;
                    return;
                }

                // Create the initial state
                componentData.Add(hash, state);
            }
        }

        internal override void InvokePrepareForGameplay(Component component)
        {
            // Check for correct type
            if ((component is T) == false)
                return;

            // Create a state
            ReplayState state = null;

            // Get the hash
            int hash = component.GetInstanceID();

            // Check for data
            if(componentData.ContainsKey(hash) == true)
            {
                // Get the state data
                state = componentData[hash];
            }
            else
            {
                return;
            }

            // Make sure we have a state even if it is empty
            if (state == null)
                state = ReplayState.pool.GetReusable();

            // Reset the sate for reading
            state.PrepareForRead();

            // Invoke method
            PrepareForGameplay(component as T, state);
        }
    }
}
