using System.Reflection;

namespace UltimateReplay.Core
{
    /// <summary>
    /// Contains data about a serialized method call.
    /// </summary>
    public struct ReplayMethodData : IReplaySerialize
    {
        // Private
        private ReplayIdentity behaviourIdentity;
        private MethodInfo targetMethod;
        private object[] methodArguments;

        // Properties
        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the replay component that recorded the method call.
        /// </summary>
        public ReplayIdentity BehaviourIdentity
        {
            get { return behaviourIdentity; }
        }

        /// <summary>
        /// The method info for the target recorded method.
        /// </summary>
        public MethodInfo TargetMethod
        {
            get { return targetMethod; }
        }

        /// <summary>
        /// The method argument values that were passed to the method.
        /// Method arguments can only be primitive types such as int.
        /// </summary>
        public object[] MethodArguments
        {
            get { return methodArguments; }
        }

        // Constructor
        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="behaviourIdentity">The identity of the behaviour component that recorded the method call</param>
        /// <param name="targetMethod">The target method information</param>
        /// <param name="methodArguments">The argument list for the target method</param>
        public ReplayMethodData(ReplayIdentity behaviourIdentity, MethodInfo targetMethod, params object[] methodArguments)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.targetMethod = targetMethod;
            this.methodArguments = methodArguments;
        }

        // Methods
        /// <summary>
        /// Serialize the method data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            // Write identity
            state.Write(behaviourIdentity);

            // Write method info
            ReplayMethods.SerializeMethodInfo(targetMethod, state);

            // Write method arguments
            ReplayMethods.SerializeMethodArguments(targetMethod, state, methodArguments);
        }

        /// <summary>
        /// Deserialize the method data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            // Read identity
            //behaviourIdentity = state.ReadSerializable<ReplayIdentity>();

            state.ReadSerializable(ref behaviourIdentity);

            // Read method info
            targetMethod = ReplayMethods.DeserializeMethodInfo(state);

            // Read method arguments
            methodArguments = ReplayMethods.DeserializeMethodArguments(targetMethod, state);
        }
    }
}
