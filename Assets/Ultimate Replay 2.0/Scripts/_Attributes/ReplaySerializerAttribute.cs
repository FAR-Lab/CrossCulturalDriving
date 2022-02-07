using System;

namespace UltimateReplay.Serializers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ReplaySerializerAttribute : Attribute
    {
        // Private
        private Type serializerType = null;

        // Properties
        public Type SerializerType
        {
            get { return serializerType; }
        }

        // Constructor
        public ReplaySerializerAttribute(Type serializerType)
        {
            this.serializerType = serializerType;
        }
    }
}
