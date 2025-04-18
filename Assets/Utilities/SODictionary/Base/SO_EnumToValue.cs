using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class SO_EnumToValue<TEnum, TValue> : ScriptableObject
{
    [System.Serializable]
    public struct EnumToValue
    {
        public TEnum TEnumValue;
        public TValue TValue;
    }
    
    public List<EnumToValue> enumToValueList = new List<EnumToValue>();
    
    public Dictionary<TEnum, TValue> EnumToValueDictionary
    {
        get
        {
            return ConvertListToDictionary(enumToValueList);
        }
    }

    private Dictionary<TEnum, TValue> ConvertListToDictionary(List<EnumToValue> list)
    {
        Dictionary<TEnum, TValue> dictionary = new Dictionary<TEnum, TValue>();
        foreach (var item in list)
        {
            dictionary[item.TEnumValue] = item.TValue;
        }
        return dictionary;
    }
    
}