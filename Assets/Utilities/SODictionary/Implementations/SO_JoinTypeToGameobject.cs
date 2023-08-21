using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SO_JoinTypeToGameobject", menuName = "ScriptableObjects/SO_JoinTypeToGameobject")]
public class SO_JoinTypeToGameobject : SO_EnumToGameobject<JoinType, GameObject>
{       
    public override List<System.Type> requiredScripts { get; set; } = new List<System.Type>()
    {   
        //typeof(SC_ClientObject)
    };
}