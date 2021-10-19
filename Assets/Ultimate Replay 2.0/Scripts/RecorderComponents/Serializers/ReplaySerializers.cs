using System;
using System.Collections.Generic;
using System.Reflection;

namespace UltimateReplay.Serializers
{
    public static class ReplaySerializers
    {
        // Private
        private static Dictionary<Type, Type> serializers = new Dictionary<Type, Type>();       // Serialize type, Serializer for type. eg: ReplayTransform, ReplayTransformSerializer
        private static List<Type> serializerIDLookup = new List<Type>();                        // Serializer type -> id by index

        // Constructor
        static ReplaySerializers()
        {
            // Get this assembly
            Assembly thisAsm = Assembly.GetExecutingAssembly();
            AssemblyName thisAsmName = thisAsm.GetName();

            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                bool checkAssembly = false;

                if (asm != thisAsm)
                {
                    // Check for assembly references this assembly - If so, the assembly may define types which use the ReplaySerializer attribute
                    foreach (AssemblyName nameInfo in asm.GetReferencedAssemblies())
                    {
                        if (nameInfo == thisAsmName)
                        {
                            checkAssembly = true;
                            break;
                        }
                    }
                }
                else
                {
                    // We are processing this assembly which does indeed have serializer types
                    checkAssembly = true;
                }

                // Should the assmebly be processed for serializers
                if (checkAssembly == false)
                    continue;

                foreach(Type type in asm.GetTypes())
                {
                    
                        // Check for attribute
                        if(type.IsDefined(typeof(ReplaySerializerAttribute), false) == true)
                        {
                        // Check for derived from replay recordable behaviour
                        if (type.IsClass == true && type.IsAbstract == false && typeof(ReplayRecordableBehaviour).IsAssignableFrom(type) == true)
                        {

                            // Register separate serializer type
                            ReplaySerializerAttribute attribute = type.GetCustomAttributes(typeof(ReplaySerializerAttribute), false)[0] as ReplaySerializerAttribute;

                            // Register the serializer type
                            serializers.Add(type, attribute.SerializerType);
                            serializerIDLookup.Add(type);
                        }
                        else
                        {
                            // Register 
                        }
                    }
                }
            }
        }

        // Methods
        public static int GetSerializerIDFromType(Type serializerType)
        {
            // Try to get index
            int index = serializerIDLookup.IndexOf(serializerType);

            if (index != -1)
                return index;

            //if(serializers.ContainsKey(serializerType) == true)
            //{
            //    int index = 0;

            //    foreach(Type type in serializers.Keys)
            //    {
            //        if (type == serializerType)
            //            return index;

            //        index++;
            //    }
            //}
            // No serializer
            return -1;
        }

        public static Type GetSerializerTypeFromID(int serializerID)
        {
            // Check for invalid id
            if (serializerID < 0)
                return null;

            // Try to resolve with quick lookup
            if (serializerID < serializerIDLookup.Count)
            {
                // Try to get serializer type
                Type serializerType = serializerIDLookup[serializerID];
                Type serializeProviderType;

                if(serializers.TryGetValue(serializerType, out serializeProviderType) == true)
                {
                    return serializeProviderType;
                }
            }
            
            // Slow lookup check must be done

            int index = 0;

            foreach (KeyValuePair<Type, Type> pair in serializers)
            {
                if (index == serializerID)
                    return pair.Value;

                index++;
            }
            return null;
        }
    }
}
