using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_SpawnTypeToGameobject", menuName = "ScriptableObjects/SO_SpawnTypeToGameobject")]
public class SO_SpawnTypeToGameobject : SO_EnumToGameobject<SpawnType, GameObject>
{
    public override List<Type> requiredScripts { get; set; } = new List<System.Type>()
    {   
        //typeof(SC_InteractableObject),
        //typeof(SC_TestTest)
    };
}