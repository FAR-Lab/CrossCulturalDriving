using System;

namespace UltimateReplay
{
    /// <summary>
    /// Attribute used to mark members as serializable using a text format.
    /// The serialized name can be specified via the attribute or the member name will be used if no name is provided.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class ReplayTextSerializeAttribute : Attribute
    {
        // Private
        private string overrideName = null;

        // Properties
        public string OverrideName
        {
            get { return overrideName; }
        }

        // Constructor
        public ReplayTextSerializeAttribute(string overrideName = null)
        {
            this.overrideName = overrideName;
        }

        // Methods
        public string GetSerializeName(string fallback)
        {
            if (overrideName != null)
                return overrideName;

            return fallback;
        }
    }
}
