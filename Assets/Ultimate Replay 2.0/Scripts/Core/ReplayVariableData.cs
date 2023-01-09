
namespace UltimateReplay.Core
{
    /// <summary>
    /// Contains all necessary data to serialize a replay variable with its value.
    /// </summary>
    public struct ReplayVariableData : IReplaySerialize
    {
        // Private
        private ReplayIdentity behaviourIdentity;
        private int variableFieldOffset;
        private ReplayState variableStateData;

        // Properties
        /// <summary>
        /// The <see cref="ReplayIdentity"/> of the <see cref="ReplayBehaviour"/> that the variable belongs to.
        /// </summary>
        public ReplayIdentity BehaviourIdentity
        {
            get { return behaviourIdentity; }
        }

        /// <summary>
        /// The field offset used to uniquley identify the variable.
        /// </summary>
        public int VariableFieldOffset
        {
            get { return variableFieldOffset; }
        }

        /// <summary>
        /// The <see cref="ReplayState"/> containing the variable value.
        /// </summary>
        public ReplayState VariableStateData
        {
            get { return variableStateData; }
        }

        // Constructor
        /// <summary>
        /// Create a new variable data instance.
        /// </summary>
        /// <param name="behaviourIdentity">The <see cref="ReplayIdentity"/> of the owning behaviour</param>
        /// <param name="variable">The <see cref="ReplayVariable"/> instance</param>
        public ReplayVariableData(ReplayIdentity behaviourIdentity, ReplayVariable variable)
        {
            this.behaviourIdentity = behaviourIdentity;
            this.variableFieldOffset = variable.FieldOffset;

            // Serialize variable
            this.variableStateData = ReplayState.pool.GetReusable();

            variable.OnReplaySerialize(variableStateData);
        }

        // Methods
        /// <summary>
        /// Serialize the variable data to the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to write to</param>
        public void OnReplaySerialize(ReplayState state)
        {
            state.Write(behaviourIdentity);
            state.Write(variableFieldOffset);
            state.Write(variableStateData);
        }

        /// <summary>
        /// Deserialize the variable data from the specified <see cref="ReplayState"/>.
        /// </summary>
        /// <param name="state">The object state to read from</param>
        public void OnReplayDeserialize(ReplayState state)
        {
            //behaviourIdentity = state.ReadSerializable<ReplayIdentity>();

            state.ReadSerializable(ref behaviourIdentity);
            variableFieldOffset = state.ReadInt32();

            // Get reusable state
            variableStateData = ReplayState.pool.GetReusable();

            state.ReadSerializable(variableStateData);
        }

        /// <summary>
        /// Try to resolve and deserialize the variable data for the specified <see cref="ReplayObject"/>.
        /// This will attempt to find the target variable on one of the observed components and will deserialize and update that variable if found.
        /// </summary>
        /// <param name="tagretObject">The <see cref="ReplayObject"/> to try and resolve</param>
        /// <returns>True if the variable was found and updated or false if not</returns>
        public bool ResolveAndDeserializeVariable(ReplayObject tagretObject)
        {
            // Try to find target behaviour
            ReplayBehaviour targetBehaviour = tagretObject.GetReplayBehaviour(behaviourIdentity);

            // Check for error
            if(targetBehaviour != null)
            {
                // Get the target variable
                ReplayVariable variable = targetBehaviour.GetReplayVariable(variableFieldOffset);

                // Check for found
                if(variable != null)
                {
                    // Deserialize the variable
                    variable.OnReplayDeserialize(variableStateData);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a value indicating whther the specified <see cref="ReplayVariable"/> corrosponds to this variable data.
        /// </summary>
        /// <param name="variable">The <see cref="ReplayVariable"/> instance to check</param>
        /// <returns>True if the variable data targets the specified variable instance or false if not</returns>
        public bool IsMatchedToVariable(ReplayVariable variable)
        {
            // Check for matching identity and field offset
            return variable.Behaviour.ReplayIdentity == behaviourIdentity
                && variable.FieldOffset == variableFieldOffset;
        }
    }
}
