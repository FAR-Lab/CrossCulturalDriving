using System.Collections.Generic;
using UnityEngine;
using UltimateReplay.Core;
using System;
using System.Reflection;
using UltimateReplay.Serializers;
using System.Runtime.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
#endif

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-firstpass")]
[assembly: InternalsVisibleTo("UltimateReplay-Editor")]

namespace UltimateReplay
{
    /// <summary>
    /// Only one instance of <see cref="ReplayObject"/> can be added to any game object. 
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]    
    [ReplaySerializer(typeof(ReplayObjectSerializer))]
    public sealed class ReplayObject : MonoBehaviour, IReplaySerialize, ISerializationCallbackReceiver
    {
        // Types
        private enum ReplayPrefabType
        {
            Unknown = 0,
            Prefab, 
            Scene,
        }

        [Serializable]
        public struct ReplayObjectReference
        {
            // Public
            public ReplayObject reference;

            // Constructor
            public ReplayObjectReference(ReplayObject obj)
            {
                this.reference = obj;
            }
        }

        // Internal
#if UNITY_EDITOR || ULTIMATEREPLAY_TRIAL        // Must be active in trail version as it is built as a dll
        public bool isObservedComponentsExpanded = false; // Now marked as public to support asmdef
#endif

        // Private        
        private static readonly List<ReplayObject> allReplayObjects = new List<ReplayObject>();

        private ReplayObjectSerializer serializer = new ReplayObjectSerializer();
        private List<ReplayVariableData> waitingVariables = new List<ReplayVariableData>();
        private List<ReplayEventData> waitingEvents = new List<ReplayEventData>();
        private List<ReplayMethodData> waitingMethods = new List<ReplayMethodData>();

        [SerializeField, HideInInspector]
        private ReplayIdentity replayIdentity = new ReplayIdentity();
        [SerializeField, HideInInspector]
        private ReplayIdentity prefabIdentity = new ReplayIdentity();

        private ReplayObjectLifecycleProvider lifecycleProvider = null;

        /// <summary>
        /// An array of <see cref="ReplayBehaviour"/> components that this object will serialize during recording.
        /// Dynamically adding replay components during recording is not supported.
        /// </summary>
        [SerializeField, HideInInspector]
        private List<ReplayRecordableBehaviour> observedComponents = new List<ReplayRecordableBehaviour>();
        [SerializeField, HideInInspector]
        private List<ReplayBehaviour> runtimeComponents = new List<ReplayBehaviour>();

        [SerializeField, HideInInspector]
        private bool isPrefab = false;
        [SerializeField, HideInInspector]
        private ReplayPrefabType prefabType = ReplayPrefabType.Unknown;

        // Properties
        public static IList<ReplayObject> AllReplayObjects
        {
            get { return allReplayObjects; }
        }

        /// <summary>
        /// Get the unique <see cref="ReplayIdentity"/> for this <see cref="ReplayObject"/>.  
        /// </summary>
        public ReplayIdentity ReplayIdentity
        {
            get { return replayIdentity; }
            set { replayIdentity = value; }
        }

        public ReplayIdentity PrefabIdentity
        {
            get { return prefabIdentity; }
        }

        public ReplayObjectLifecycleProvider LifecycleProvider
        {
            get { return lifecycleProvider; }
            internal set { lifecycleProvider = value; }
        }

        /// <summary>
        /// Returns true when this game object is a prefab asset.
        /// Returns false when this game object is a scene object or prefab instance.
        /// </summary>
        public bool IsPrefab
        {
            get { return isPrefab; }// gameObject.scene.rootCount == 0; }
        }

        public IList<ReplayRecordableBehaviour> ObservedComponents
        {
            get { return observedComponents; }
        }

        // Methods
        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void Awake()
        {
            UpdateRuntimeComponents();

            //UpdatePrefabLinks();

            //Check if we have instantiated the object and need to generate new ids
            if (isPrefab == true && ReplayIdentity.IsUnique(replayIdentity, false) == false)
            {
                // New id's are required for prefab instances
                ForceRegenerateIdentityWithObservedComponents();
            }
        }

        public void Start()
        {
            //// Check if we have instantiated the object and need to generate new ids
            //if (IsPrefab == true && ReplayIdentity.IsUnique(replayIdentity, false) == false)
            //{
            //    // New id's are required for prefab instances
            //    ForceRegenerateIdentityWithObservedComponents();
            //}
        }

        public void Update()
        {
            // Only run in editor non-play mode
            if (Application.isPlaying == false)
            {
                // Check if components have been removed
                if (CheckComponentListIntegrity() == false)
                    RebuildComponentList();

                // Update prefab
                UpdatePrefabLinks();
            }
            else
            {
                // Update all variables
                for (int i = 0; i < runtimeComponents.Count; i++)
                    if (runtimeComponents[i] != null)
                        runtimeComponents[i].UpdateReplayVariables();
            }
        }

        public void OnEnable()
        {            
            // Register replay object
            allReplayObjects.Add(this);
        }

        public void OnDisable()
        {
            // Unregister replay object
            if (allReplayObjects.Contains(this) == true)
                allReplayObjects.Remove(this);
        }

        /// <summary>
        /// Called by Unity.
        /// </summary>
        public void OnDestroy()
        {          
        }

        /// <summary>
        /// Called by Unity editor.
        /// </summary>
        public void Reset()
        {
            UpdatePrefabLinks();
            RebuildComponentList();
        }

        /// <summary>
        /// Called by Unity editor
        /// </summary>
        public void OnValidate()
        {
            if(Event.current != null)
            {
                if((Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Duplicate") ||
                    (Event.current.type == EventType.ExecuteCommand && Event.current.commandName == "Paste") ||
                    (Event.current.type == EventType.ValidateCommand && Event.current.commandName == "Paste"))
                {
                    ForceRegenerateIdentityWithObservedComponents();
                }
            }
        }

        public void UpdateRuntimeComponents()
        {
            //if (Application.isPlaying == true)
            {
                if (runtimeComponents.Count == 0)
                {
                    // Get all behaviour components
                    foreach (ReplayBehaviour behaviour in GetComponentsInChildren<ReplayBehaviour>())
                    {
                        // Only get comoponents which are managed by this component
                        if (behaviour.ReplayObject == this)
                        {
                            // Register the component for receiving replay variable updates, events and method calls
                            runtimeComponents.Add(behaviour);
                        }
                    }
                }
            }
        }

        public void ForceRegenerateIdentity()
        {
            // Generate replay id
            ReplayIdentity.Generate(ref replayIdentity);
        }

        public void ForceRegenerateIdentityWithObservedComponents()
        {
            // Generate a new identity
            ForceRegenerateIdentity();

            // Regenerate observed component id's
            foreach (ReplayRecordableBehaviour behaviour in observedComponents)
            {
                behaviour.ForceRegenerateIdentity();
            }
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            UpdatePrefabLinks();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {

            //// Do nothing
            //UpdatePrefabLinks();
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplayObject"/> should serialize its replay data. 
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to serialize the data to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            // Reset the serializer
            serializer.Reset();

            // Request runtime behaviours to submit variable data
            foreach (ReplayBehaviour behaviour in runtimeComponents)
                if(behaviour != null)
                    behaviour.SubmitReplayVariables();


            // Generate serialize flags
            ReplayObjectSerializer.ReplayObjectSerializeFlags flags = 0;

            if (observedComponents.Count > 0) flags |= ReplayObjectSerializer.ReplayObjectSerializeFlags.Components;
            if (waitingVariables.Count > 0) flags |= ReplayObjectSerializer.ReplayObjectSerializeFlags.Variables;
            if (waitingEvents.Count > 0) flags |= ReplayObjectSerializer.ReplayObjectSerializeFlags.Events;
            if (waitingMethods.Count > 0) flags |= ReplayObjectSerializer.ReplayObjectSerializeFlags.Methods;


            // Set the serialize flags
            serializer.SerializeFlags = flags;

            // Set the prefab identity
            serializer.PrefabIdentity = prefabIdentity;
            
            foreach (ReplayRecordableBehaviour behaviour in observedComponents)
            {
                // Check for invalid behaviour
                if (behaviour == null)
                    continue;

                // Get a reusable state
                ReplayState stateData = ReplayState.pool.GetReusable();

                // Serialize the component
                behaviour.OnReplaySerialize(stateData);

                // Get the recorder component type
                Type behaviourType = behaviour.GetType();

                // Get serializer id
                int serializerID = ReplaySerializers.GetSerializerIDFromType(behaviourType);

                // Add to serializer
                if (serializerID == -1)
                {
                    serializer.ComponentStates.Add(new ReplayComponentData(behaviour.ReplayIdentity, behaviourType, stateData));
                }
                else
                {
                    serializer.ComponentStates.Add(new ReplayComponentData(behaviour.ReplayIdentity, serializerID, stateData));
                }
            }


            // Add variables to be serialized
            foreach (ReplayVariableData variable in waitingVariables)
                serializer.VariableStates.Add(variable);

            // Add events to be serialized
            foreach (ReplayEventData evt in waitingEvents)
                serializer.EventStates.Add(evt);

            // Add methods to be serialized
            foreach (ReplayMethodData method in waitingMethods)
                serializer.MethodStates.Add(method);

            // Reset waiting collections
            waitingVariables.Clear();
            waitingEvents.Clear();
            waitingMethods.Clear();

            // Run the serializer
            serializer.OnReplaySerialize(state);
        }

        /// <summary>
        /// Called by the replay system when this <see cref="ReplayObject"/> should deserialize its replay data. 
        /// </summary>
        /// <param name="state">The <see cref="ReplayState"/> to deserialize the data from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Run the serializer
            serializer.OnReplayDeserialize(state);

            // Get the flags
            ReplayObjectSerializer.ReplayObjectSerializeFlags flags = serializer.SerializeFlags;
            
            // Check for components
            if ((flags & ReplayObjectSerializer.ReplayObjectSerializeFlags.Components) != 0) DeserializeReplayComponents(state, serializer.ComponentStates);
            if ((flags & ReplayObjectSerializer.ReplayObjectSerializeFlags.Variables) != 0) DeserializeReplayVariables(state, serializer.VariableStates);
            if ((flags & ReplayObjectSerializer.ReplayObjectSerializeFlags.Events) != 0) DeserializeReplayEvents(state, serializer.EventStates);
            if ((flags & ReplayObjectSerializer.ReplayObjectSerializeFlags.Methods) != 0) DeserializeReplayMethods(state, serializer.MethodStates);

            // Reset the serializer
            serializer.Reset();
        }

        #region VariablesEventsMethods
        public void RecordReplayVariable(ReplayIdentity senderIdentity, ReplayVariable replayVariable)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Push variable
            waitingVariables.Add(new ReplayVariableData(senderIdentity, replayVariable)); 
        }

        public void RecordReplayEvent(ReplayIdentity senderIdentity, ushort eventID, ReplayState eventData = null)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Push variable
            waitingEvents.Add(new ReplayEventData(senderIdentity, eventID, eventData));
        }

        public void Call(ReplayIdentity senderIdentity, Action method)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method));

            // Call method
            method.Invoke();
        }

        public void Call<T>(ReplayIdentity senderIdentity, Action<T> method, T arg)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg));

            // Call method
            method.Invoke(arg);
        }

        public void Call<T0, T1>(ReplayIdentity senderIdentity, Action<T0, T1> method, T0 arg0, T1 arg1)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1));

            // Call method
            method.Invoke(arg0, arg1);
        }

        public void Call<T0, T1, T2>(ReplayIdentity senderIdentity, Action<T0, T1, T2> method, T0 arg0, T1 arg1, T2 arg2)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1, arg2));

            // Call method
            method.Invoke(arg0, arg1, arg2);
        }

        public void Call<T0, T1, T2, T3>(ReplayIdentity senderIdentity, Action<T0, T1, T2, T3> method, T0 arg0, T1 arg1, T2 arg2, T3 arg3)
        {
            // Check for valid id
            if (senderIdentity.IsValid == false)
                return;

            // Make sure the method is recordable
            if (CheckMethodRecordable(senderIdentity, method.Method) == false)
                return;

            // Push method
            waitingMethods.Add(new ReplayMethodData(senderIdentity, method.Method, arg0, arg1, arg2, arg3));

            // Call method
            method.Invoke(arg0, arg1, arg2, arg3);
        }

        private bool CheckMethodRecordable(ReplayIdentity senderIdentity, MethodInfo targetMethod)
        {
            // Check attribute
            if(targetMethod.IsDefined(typeof(ReplayMethodAttribute)) == false)
            {
                Debug.LogErrorFormat("The method '{0}' cannot be recorded because it does not have the 'ReplayMethod' attribute", targetMethod);
                return false;
            }

            // Check base type
            if(typeof(ReplayBehaviour).IsAssignableFrom(targetMethod.DeclaringType) == false)
            {
                Debug.LogErrorFormat("The method '{0}' cannot be recorded because it is not declared in a type that inherits from 'ReplayBehaviour'", targetMethod);
                return false;
            }

            return true;
        }
        #endregion

        /// <summary>
        /// Returns a value indicating whether the specified recorder component is observed by this <see cref="ReplayObject"/>.
        /// </summary>
        /// <param name="component">The recorder component to check</param>
        /// <returns>True if the component is observed or false if not</returns>
        public bool IsComponentObserved(ReplayRecordableBehaviour component)
        {
            return observedComponents.Contains(component);
        }
        
        /// <summary>
        /// Forces the object to refresh its list of observed components.
        /// Observed components are components which inherit from <see cref="ReplayBehaviour"/> and exist on either this game object or a child of this game object. 
        /// </summary>
        public void RebuildComponentList()
        {
            observedComponents.Clear();

            // Process all child behaviour scripts
            foreach (ReplayRecordableBehaviour behaviour in GetComponentsInChildren<ReplayRecordableBehaviour>(true))
            {
                // Check for deleted components
                if (behaviour == null)
                    continue;

                // Only add the script if it is not marked as ignored
                if (behaviour.GetType().IsDefined(typeof(ReplayIgnoreAttribute), true) == false)
                {
                    // Check for sub object handlers
                    if(behaviour.gameObject != gameObject)
                    {
                        GameObject current = behaviour.gameObject;
                        bool skipBehaviour = false;                        

                        while(true)
                        {
                            if (current.GetComponent<ReplayObject>() != null)
                            {
                                skipBehaviour = true;
                                break;
                            }

                            if (current.transform.parent == null || current.transform.parent == transform)
                                break;

                            // Move up hierarchy
                            current = current.transform.parent.gameObject;
                        }

                        if (skipBehaviour == true)
                            continue;
                    }


                    // Update object
                    if (behaviour.ReplayObject == null || behaviour.ReplayObject == this)
                    {
                        // Add script
                        observedComponents.Add(behaviour);

#if UNITY_EDITOR
                        // Update the behaviour
                        behaviour.UpdateManagingObject();
#endif
                    }
                }
            }

            // Rebuild all parents
            foreach (ReplayObject obj in GetComponentsInParent<ReplayObject>())
                if (obj != this && obj != null)
                    obj.RebuildComponentList();
        }

        /// <summary>
        /// Returns a value indicating whether the observed component list is valid or needs o be rebuilt.
        /// </summary>
        /// <returns>True if the collection is valid or false if not</returns>
        public bool CheckComponentListIntegrity()
        {
            foreach (ReplayRecordableBehaviour observed in observedComponents)
                if (observed == null)
                    return false;

            return true;
        }

        public void UpdatePrefabLinks()
        {
            
            if(IsPrefab == false)
            {

            }

            //if (Application.isPlaying == false)
            {
                //#if UNITY_EDITOR
                //                if (EditorApplication.isPlayingOrWillChangePlaymode == true)
                //                    return;
                //#endif


                if (Application.isPlaying == true)
                    return;

                if (prefabType == ReplayPrefabType.Unknown ||
                    (prefabType == ReplayPrefabType.Prefab && gameObject.scene.rootCount != 0) ||
                    (prefabType == ReplayPrefabType.Scene && gameObject.scene.rootCount == 0))
                {
                    isPrefab = (gameObject.scene.rootCount == 0);
                    prefabType = (gameObject.scene.rootCount == 0) ? ReplayPrefabType.Prefab : ReplayPrefabType.Scene;
                }
            }

#if UNITY_EDITOR
            //if (Application.isPlaying == false)
            {
                //bool oldIsPrefab = isPrefab;

                //// Generate the id
                ////replayIdentity.Generate();

                //// Set prefab flag
                //if (Application.isPlaying == false)
                //{
                //    isPrefab = isActiveAndEnabled == false && PrefabUtility.GetPrefabType(gameObject) == PrefabType.Prefab;  //(gameObject.scene.name == null);
                //}
                //// Check if the object has been spawed into the scene
                //if(oldIsPrefab == true && isPrefab == false)
                //{
                //    // Reset prefab identity
                //    prefabIdentity = ReplayIdentity.invalid;

                //    // Generate a new replay id
                //    ReplayIdentity.Generate(ref replayIdentity);
                //}

                //// Get the prefab type
                //PrefabType type = PrefabUtility.GetPrefabType(gameObject);

                //// Disallow scene objects that are active
                //if (isActiveAndEnabled == false && type == PrefabType.Prefab)
                //{
                //    // Store the prefab name
                //    prefabIdentity = gameObject.name;
                //}
                //else
                //{
                //    // The object is not a prefab
                //    prefabIdentity = string.Empty;
                //}
            }
//#else
//            Debug.LogWarning("UpdatePrefabLinks can only be called inside the Unity editor. Calling at runtime will have no effect");
#endif
        }

        private void DeserializeReplayComponents(ReplayState state, IList<ReplayComponentData> components)
        {
            //foreach(ReplayComponentData componentData in components)
            for(int i = 0; i < components.Count; i++)
            {
                ReplayComponentData componentData = components[i];

                // Try to get behaviour
                ReplayRecordableBehaviour behaviour = GetReplayBehaviour(componentData.BehaviourIdentity) as ReplayRecordableBehaviour;

                // Check for found
                if(behaviour != null)
                {
                    // Get the state data
                    ReplayState behaviourState = componentData.ComponentStateData;

                    try
                    {
                        // Check for no data
                        if (behaviourState.Size == 0)
                            continue;

                        // Prepare for read
                        behaviourState.PrepareForRead();

                        // Run deserialize
                        behaviour.OnReplayDeserialize(behaviourState);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void DeserializeReplayVariables(ReplayState state, IList<ReplayVariableData> variables)
        {
            foreach(ReplayVariableData variableData in variables)
            {
                // Try to get behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(variableData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Try to resolve the variable
                    ReplayVariable targetVariable = behaviour.GetReplayVariable(variableData.VariableFieldOffset);

                    // Check for found
                    if(targetVariable != null)
                    {
                        // Get the state data
                        ReplayState variableState = variableData.VariableStateData;

                        // Prepare for read
                        variableState.PrepareForRead();

                        // Run deserializer
                        targetVariable.OnReplayDeserialize(variableState);
                    }
                }
            }
        }

        private void DeserializeReplayEvents(ReplayState state, IList<ReplayEventData> events)
        {
            foreach(ReplayEventData eventData in events)
            {
                // Get target behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(eventData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Send the event
                    try
                    {
                        // Get the event data
                        ReplayState eventState = eventData.EventState;

                        // Dont pass null state to the user method
                        if (eventState == null)
                            eventState = ReplayState.pool.GetReusable();

                        // Prepare for read
                        eventState.PrepareForRead();

                        // Safe call event method
                        behaviour.OnReplayEvent(eventData.EventID, eventState);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        private void DeserializeReplayMethods(ReplayState state, IList<ReplayMethodData> methods) 
        {
            foreach(ReplayMethodData methodData in methods)
            {
                // Get target behaviour
                ReplayBehaviour behaviour = GetReplayBehaviour(methodData.BehaviourIdentity);

                // Check for found
                if(behaviour != null)
                {
                    // Invoke the method
                    try
                    {
                        // Call tje method on the behaviour
                        methodData.TargetMethod.Invoke(behaviour, methodData.MethodArguments);
                    }
                    catch(Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }

        internal void RegisterRuntimeBehaviour(ReplayBehaviour behaviour)
        {
            if (behaviour != null && runtimeComponents.Contains(behaviour) == false)
                runtimeComponents.Add(behaviour);
        }

        internal void UnregisterRuntimeBehaviour(ReplayBehaviour behaviour)
        {
            if (behaviour != null && runtimeComponents.Contains(behaviour) == true)
                runtimeComponents.Remove(behaviour);
        }

        /// <summary>
        /// Get the <see cref="ReplayBehaviour"/> observed by this <see cref="ReplayObject"/> with the specified <see cref="ReplayIdentity"/>.
        /// </summary>
        /// <param name="replayIdentity"></param>
        /// <returns></returns>
        public ReplayBehaviour GetReplayBehaviour(ReplayIdentity replayIdentity)
        {
            foreach(ReplayBehaviour behaviour in runtimeComponents)
            {
                if (behaviour != null && behaviour.ReplayIdentity == replayIdentity)
                    return behaviour;
            }
            return null;
        }

        public static bool CloneReplayObjectIdentity(GameObject cloneFromObject, GameObject cloneToObject)
        {
            // Get replay object components
            ReplayObject[] from = cloneFromObject.GetComponentsInChildren<ReplayObject>();
            ReplayObject[] to = cloneToObject.GetComponentsInChildren<ReplayObject>();

            // Make sure components are found
            if (from.Length != to.Length)
                return false;

            bool cloned = true;

            for(int i = 0; i < from.Length; i++)
            {
                // Clone each replay object component
                if (CloneReplayObjectIdentity(from[i], to[i]) == false)
                    cloned = false;
            }

            // Call through
            return cloned;
        }

        public static bool CloneReplayObjectIdentity(ReplayObject cloneFromObject, ReplayObject cloneToObject)
        {
            // Check for same object
            if (cloneFromObject == cloneToObject)
                return false;

            // Replay components must match
            if (cloneFromObject.observedComponents.Count != cloneToObject.observedComponents.Count)
                return false;

            // Clone object id
            cloneToObject.replayIdentity = new ReplayIdentity(cloneFromObject.replayIdentity);

            for(int i = 0; i < cloneFromObject.observedComponents.Count; i++)
            {
                // Check for destroyed components
                if (cloneToObject.observedComponents[i] == null || cloneFromObject.observedComponents[i] == null)
                    continue;

                // Clone component id
                cloneToObject.observedComponents[i].ReplayIdentity = new ReplayIdentity(cloneFromObject.observedComponents[i].ReplayIdentity);
            }

            return true;
        }
    }
}
