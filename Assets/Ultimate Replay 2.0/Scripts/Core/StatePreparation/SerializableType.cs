using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateReplay.Core.StatePreparer
{
    [Serializable]
    public class SerializableType
    {
        // Types
        private class SerializableTypeError { }

        // Private
        [SerializeField, HideInInspector]
        private string assemblyQualifiedTypeName = "";

        private Type systemTypeInstance = null;

        // Properties
        public Type SystemType
        {
            get
            {
                ResolveType();
                return systemTypeInstance;
            }
        }

        // Constructor
        public SerializableType() { }

        public SerializableType(Type systemType)
        {
            this.assemblyQualifiedTypeName = systemType.AssemblyQualifiedName;
            this.systemTypeInstance = systemType;
        }

        // Methods
        public bool ResolveType()
        {
            // Check if type is already assigned
            if (systemTypeInstance != null) 
                return true;

            // Try to get type
            systemTypeInstance = Type.GetType(assemblyQualifiedTypeName, false);

            // Check for error
            if (systemTypeInstance == null)
                systemTypeInstance = typeof(SerializableTypeError);

            // Check if the type was resolved
            return systemTypeInstance != typeof(SerializableTypeError);
        }

        public static implicit operator SerializableType(Type systemType)
        {
            return new SerializableType(systemType);
        }
    }
}
