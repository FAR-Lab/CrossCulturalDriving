using System;
using System.Collections.Generic;
using System.Reflection;
using UltimateReplay.Core;
using UnityEngine;

#if ULTIMATEREPLAY_JSON
using Newtonsoft.Json;

namespace UltimateReplay.Storage
{
    internal sealed class ReplayStreamJsonSerializer
    {
        // Types
        private struct SerializeMember
        {
            // Public
            public FieldInfo field;
            public PropertyInfo property;
            public string serializeName;

            // Constructor
            public SerializeMember(MemberInfo member, string displayName)
            {
                field = null;
                property = null;
                serializeName = displayName;

                if (member is FieldInfo)
                    field = member as FieldInfo;

                if (member is PropertyInfo)
                    property = member as PropertyInfo;
            }

            // Methods
            public object GetValue(object instance)
            {
                if (field != null)
                    return field.GetValue(instance);

                if (property != null)
                    return property.GetValue(instance, null);

                return null;
            }
        }

        // Private
        private Dictionary<Type, MethodInfo> serializeMethods = null;
        private Dictionary<Type, List<SerializeMember>> serializeMembersCache = null;
        private JsonWriter writer = null;

        // Constructor
        public ReplayStreamJsonSerializer(JsonWriter writer)
        {
            this.writer = writer;

            serializeMethods = new Dictionary<Type, MethodInfo>();
            serializeMembersCache = new Dictionary<Type, List<SerializeMember>>();
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if(method.Name.StartsWith("Encode") == true)
                {
                    ParameterInfo[] parameters = method.GetParameters();

                    if(parameters.Length == 1)
                    {
                        serializeMethods.Add(parameters[0].ParameterType, method);
                    }
                }
            }
        }

        // Methods
        public void EncodeObject(object obj)
        {
            // Get the object type
            Type type = obj.GetType();

            // Get the members list
            List<SerializeMember> members = GetOrCreateMemberLookupForType(type);

            // Process all members
            foreach (SerializeMember member in members)
            {
                // Write name
                writer.WritePropertyName(member.serializeName);

                // Write value
                EncodeValue(member.GetValue(obj));
            }
        }

        public void EncodeValue(object value)
        {
            MethodInfo encodeMethod = null;

            // Try to get the custom encode method
            if(value != null && serializeMethods.TryGetValue(value.GetType(), out encodeMethod) == true)
            {
                // Encode the property
                encodeMethod.Invoke(this, new object[] { value });
            }
            else
            {
                if (value != null && value.GetType().IsArray == true)
                {
                    writer.WriteStartArray();
                    {
                        Array arr = value as Array;

                        foreach(object val in arr)
                        {
                            EncodeValue(val);
                        }
                    }
                    writer.WriteEndArray();
                }
                else
                {
                    // Let the json util handle the value but it may cause an unsupported exception
                    writer.WriteValue(value);
                }
            }
        }

        private void EncodeValue(ReplayIdentity id)
        {
            writer.WriteValue(id.IDValue);
        }

        private void EncodeValue(Vector2 vec)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(vec.x);
                writer.WritePropertyName("Y"); writer.WriteValue(vec.y);
            }
            writer.WriteEndObject();
        }

        private void EncodeValue(Vector3 vec)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(vec.x);
                writer.WritePropertyName("Y"); writer.WriteValue(vec.y);
                writer.WritePropertyName("Z"); writer.WriteValue(vec.z);
            }
            writer.WriteEndObject();
        }

        private void EncodeValue(Quaternion quat)
        {
            writer.WriteStartObject();
            {
                writer.WritePropertyName("X"); writer.WriteValue(quat.x);
                writer.WritePropertyName("Y"); writer.WriteValue(quat.y);
                writer.WritePropertyName("Z"); writer.WriteValue(quat.z);
                writer.WritePropertyName("W"); writer.WriteValue(quat.w);
            }
            writer.WriteEndObject();
        }

        private List<SerializeMember> GetOrCreateMemberLookupForType(Type type)
        {
            // Try to get cached fields
            List<SerializeMember> serializeMembers = null;

            if (serializeMembersCache.TryGetValue(type, out serializeMembers) == false)
            {
                // We need to build the cache for the type
                serializeMembers = new List<SerializeMember>();

                // Process all fields
                foreach (MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (member is FieldInfo || member is PropertyInfo)
                    {
                        if (member.IsDefined(typeof(ReplayTextSerializeAttribute), false) == true)
                        {
                            ReplayTextSerializeAttribute attrib = member.GetCustomAttributes(typeof(ReplayTextSerializeAttribute), false)[0] as ReplayTextSerializeAttribute;

                            serializeMembers.Add(new SerializeMember(member, attrib.GetSerializeName(member.Name)));
                        }
                    }
                }

                // Cache results
                serializeMembersCache.Add(type, serializeMembers);
            }

            return serializeMembers;
        }
    }
}
#endif
