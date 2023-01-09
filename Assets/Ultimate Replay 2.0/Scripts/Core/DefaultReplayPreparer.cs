using System;
using System.Collections.Generic;
using UltimateReplay.Core.StatePreparer;
using UnityEngine;

namespace UltimateReplay.Core
{
    /// <summary>
    /// The default <see cref="IReplayPreparer"/> used by Ultimate Replay to prepare game objects for gameplay and playback.
    /// </summary>
    [Serializable]
    public class DefaultReplayPreparer : IReplayPreparer, ISerializationCallbackReceiver
    {
        // Types
        [Serializable]
        public class ComponentPreparerSettings
        {
            // Public
            public SerializableType componentPreparerType;
            public bool enabled = true;
        }

        // Private
        private static HashSet<ComponentPreparer> preparers = new HashSet<ComponentPreparer>();

        // Private
        [SerializeField, HideInInspector]
        private List<SerializableType> skipTypes = new List<SerializableType>
        {
            typeof(ReplayObject),
            typeof(ReplayBehaviour),
            typeof(Camera),
            typeof(AudioSource),
            typeof(ParticleSystem),
        };

        [SerializeField, HideInInspector]
        private List<ComponentPreparerSettings> preparerSettings = new List<ComponentPreparerSettings>();

        // Properties
        public IList<SerializableType> SkipTypes
        {
            get { return skipTypes; }
        }

        public IList<ComponentPreparerSettings> PreparerSettings
        {
            get { return preparerSettings; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        static DefaultReplayPreparer()
        {
            // Load all preparers
            ComponentPreparer.InitializePreparers();
        }

        // Methods
        /// <summary>
        /// Prepare the specified replay object for playback mode.
        /// </summary>
        /// <param name="replayObject">The replay object to prepare</param>
        public virtual void PrepareForPlayback(ReplayObject replayObject)
        {
            // Find all components on the object
            foreach (Component component in replayObject.GetComponentsInChildren<Component>())
            {
                // Make sure the component is still alive
                if (component == null)
                    return;

                bool skip = false;

                // Check if the component should be prepared or skipped
                foreach (SerializableType skipType in skipTypes)
                {
                    // Check if the component is a skip type or child of
                    if (skipType.SystemType.IsInstanceOfType(component))
                    {
                        // Set the skip flag
                        skip = true;
                        break;
                    }
                }

                // Check if we should skip the component
                if (skip == true)
                    continue;

                // Get the component type
                Type componentType = component.GetType();

                // Try to find a preparer
                ComponentPreparer preparer = ComponentPreparer.FindPreparer(componentType);

                // Check for error
                if (preparer == null || preparer.enabled == false)
                    continue;

                // Prepare the component
                preparer.InvokePrepareForPlayback(component);
            }
        }

        /// <summary>
        /// Prepare the specified replay object for gameplay mode.
        /// </summary>
        /// <param name="replayObject">The replay object to prepare</param>
        public virtual void PrepareForGameplay(ReplayObject replayObject)
        {
            // Find all components on the object
            foreach (Component component in replayObject.GetComponentsInChildren<Component>())
            {
                // Make sure the component is still alive
                if (component == null)
                    return;

                bool skip = false;

                // Check if the component should be prepared or skipped
                foreach(SerializableType skipType in skipTypes)
                {
                    // Check if the component is a skip type or child of
                    if (skipType.SystemType.IsInstanceOfType(component))
                    {
                        // Set the skip flag
                        skip = true;
                        break;
                    }
                }

                // Check if we should skip the component
                if (skip == true)
                    continue;

                // Get the component type
                Type componentType = component.GetType();

                // Try to find a preparer
                ComponentPreparer preparer = ComponentPreparer.FindPreparer(componentType);

                // Check for error
                if (preparer == null || preparer.enabled == false)
                    continue;

                // Prepare the component
                preparer.InvokePrepareForGameplay(component);
            }
        }

        public bool HasSkipType(Type systemType)
        {
            foreach(SerializableType type in skipTypes)
            {
                if (type.SystemType == systemType)
                    return true;
            }
            return false;
        }

        public void OnBeforeSerialize()
        {
            // Create options
            foreach (ComponentPreparer preparer in preparers)
            {
                // Check for component settings added
                bool exists = preparerSettings.Exists(p => p.componentPreparerType.SystemType == preparer.Attribute.componentType);

                // Check for exists
                if (exists == false)
                {
                    preparerSettings.Add(new ComponentPreparerSettings
                    {
                        componentPreparerType = preparer.Attribute.componentType,
                        enabled = true,
                    });
                }
            }
        }

        public void OnAfterDeserialize()
        {
            foreach(ComponentPreparer preparer in preparers)
            {
                // Check for matching component
                ComponentPreparerSettings setting = preparerSettings.Find(p => p.componentPreparerType.SystemType == preparer.Attribute.componentType);

                if (setting != null)
                {
                    // Apply settings
                    preparer.enabled = setting.enabled;
                }
            }
        }

        public DefaultReplayPreparer CreateInstance()
        {
            DefaultReplayPreparer instance = new DefaultReplayPreparer();

            instance.skipTypes.Clear();
            instance.skipTypes.AddRange(skipTypes);

            instance.preparerSettings.Clear();
            instance.preparerSettings.AddRange(PreparerSettings);

            return instance;
        }
    }
}
