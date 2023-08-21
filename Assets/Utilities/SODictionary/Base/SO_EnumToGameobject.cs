using UnityEngine;
using System;
using System.Collections.Generic;

public abstract class SO_EnumToGameobject<TEnum, TGameObject> : SO_EnumToValue<TEnum, TGameObject>
{
    public abstract List<Type> requiredScripts { get; set; }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CheckGameObjectsForRequiredScripts();
    }

    private void CheckGameObjectsForRequiredScripts()
    {
        for (int i = 0; i < enumToValueList.Count; i++)
        {
            GameObject go = enumToValueList[i].TValue as GameObject;
            if (go)
            {
                bool missingScript = false;
                foreach (Type type in requiredScripts)
                {
                    if (go.GetComponent(type) == null)
                    {
                        Debug.LogError($"GameObject {go.name} does not contain required script: {type.Name}");
                        missingScript = true;
                    }
                }
                if (missingScript)
                {
                    //Debug.LogError($"Setting GameObject {go.name} to null");
                    enumToValueList[i] = new EnumToValue
                    {
                        TEnumValue = enumToValueList[i].TEnumValue,
                        TValue = default(TGameObject)
                    };
                }
            }
        }
    }
#endif
}