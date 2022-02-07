using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UltimateReplay.Core
{
    internal class ReplayMethods
    {
        // Private
        private static readonly byte serializeMethodID = 56;
        private static HashSet<MethodInfo> replayMethods = new HashSet<MethodInfo>();

        // Constructor
        static ReplayMethods()
        {
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type type in asm.GetTypes())
                {
                    if (type.IsClass == true && type.IsAbstract == false && typeof(ReplayBehaviour).IsAssignableFrom(type) == true)
                    {
                        foreach (MethodInfo method in type.GetMethods())
                        {
                            // Register the replay method
                            if (method.IsDefined(typeof(ReplayMethodAttribute), false) == true)
                                replayMethods.Add(method);
                        }
                    }
                }
            }
        }

        // Methods
        public static bool SerializeMethodInfo(MethodInfo method, ReplayState state)
        {
            // Check for replay method
            if(replayMethods.Contains(method) == false)
            {
                Debug.LogWarningFormat("The method '{0}' cannot be serialized because it is not decorated with the 'ReplayMethod' attribute", method);
                return false;
            }

            // Write method identifier
            state.Write(serializeMethodID);

            // Write the declaring type
            state.Write(method.DeclaringType.AssemblyQualifiedName);

            // Write the method name
            state.Write(method.Name);

            // Get param list
            ParameterInfo[] parameters = method.GetParameters();

            // Write parameter count
            state.Write((byte)parameters.Length);

            // Write the parameter types
            for(int i = 0; i < parameters.Length; i++)
            {
                // Get the parameter type
                Type paramType = parameters[i].ParameterType;

                // Check if parameter is serializable
                if(ReplayState.IsTypeSerializable(paramType) == false)
                {
                    Debug.LogWarningFormat("The replay method '{0}' cannot be recorded because the parameter type '{1}' is not serializable", method, paramType);
                    return false;
                }

                // Write the parameter type name
                state.Write(paramType.AssemblyQualifiedName);
            }

            return true;
        }

        public static bool SerializeMethodArguments(MethodInfo method, ReplayState state, params object[] args)
        {
            // Get method params
            ParameterInfo[] parameters = method.GetParameters();

            // Write the argument length in case of optional arguments
            state.Write((ushort)parameters.Length);

            if(args.Length <= parameters.Length)
            {
                for(int i = 0; i < args.Length; i++)
                {
                    // Get the method used to serialize the parameters
                    MethodInfo serializeMethod = ReplayState.GetSerializeMethod(parameters[i].ParameterType);

                    // Failed to get write method
                    if (serializeMethod == null)
                        return false;

                    // Write the parameter
                    serializeMethod.Invoke(state, new object[] { args[i] });
                }
            }
            return true;
        }

        public static MethodInfo DeserializeMethodInfo(ReplayState state)
        {
            // Check for identifier
            if (state.ReadByte() != serializeMethodID)
                return null;

            // Get declaring type name
            string assemblyQualifiedName = state.ReadString();

            // Try to resolve type
            Type resolvedType = Type.GetType(assemblyQualifiedName);

            // Check for failure
            if (resolvedType == null)
                return null;

            // Get method name
            string methodName = state.ReadString();

            // Get parameter count
            byte paramsLength = state.ReadByte();

            Type[] parameterTypes = new Type[paramsLength];

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                // Get the full name for the parameter type
                string assemblyQualifiedParemterName = state.ReadString();

                // Try to resolve
                parameterTypes[i] = Type.GetType(assemblyQualifiedParemterName);

                if (parameterTypes[i] == null)
                    return null;
            }

            // Check for no parameters
            if (paramsLength == 0)
                parameterTypes = Type.EmptyTypes;

            // Try to resolve the method
            return resolvedType.GetMethod(methodName, parameterTypes);
        }

        public static object[] DeserializeMethodArguments(MethodInfo method, ReplayState state)
        {
            // Get method params
            ParameterInfo[] parameters = method.GetParameters();

            // Read arg length
            ushort length = state.ReadUInt16();

            object[] arguments = new object[length];

            for (int i = 0; i < length; i++)
            {
                // Get the method used to serialize the parameters
                MethodInfo deserializeMethod = ReplayState.GetDeserializeMethod(parameters[i].ParameterType);

                // Failed to get write method
                if (deserializeMethod == null)
                    return null;

                // Write the parameter
                arguments[i] = deserializeMethod.Invoke(state, null);
            }

            return arguments;
        }
    }
}
