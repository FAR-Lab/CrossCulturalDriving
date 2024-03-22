using System;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SO_EnumToValue<,>), true)]
public class Editor_EnumToValue : Editor
{
    private void OnEnable()
    {
        PrepopulateEnumList(); 
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();        
        
        DisplayEnumWithGameObject();

        serializedObject.ApplyModifiedProperties();
    }

    private void DisplayEnumWithGameObject()
    {
        ScriptableObject so = target as ScriptableObject;
        if (so == null)
        {
            return;
        }

        SerializedProperty listProperty = serializedObject.FindProperty("enumToValueList");
        GUILayout.Label("Enum - Value Pairs:");

        for (int i = 0; i < listProperty.arraySize; i++)
        {
            SerializedProperty itemProperty = listProperty.GetArrayElementAtIndex(i);
            SerializedProperty enumProperty = itemProperty.FindPropertyRelative("TEnumValue");
            SerializedProperty goProperty = itemProperty.FindPropertyRelative("TValue");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(enumProperty.enumNames[enumProperty.enumValueIndex], GUILayout.Width(150));
            goProperty.objectReferenceValue = EditorGUILayout.ObjectField(goProperty.objectReferenceValue, typeof(Object), false);
            EditorGUILayout.EndHorizontal();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void PrepopulateEnumList()
    {
        ScriptableObject so = target as ScriptableObject;
        if (so == null)
        {
            return;
        }
        
        // if there are already enumToValueList items, don't prepopulate
        SerializedProperty listProperty = serializedObject.FindProperty("enumToValueList");
        if (listProperty.arraySize > 0)
        {
            return;
        }

        Type baseType = so.GetType().BaseType; // base type refers to SO_EnumToGameobject<TEnum, TValue>
        Type enumType = baseType.GetGenericArguments()[0];
        PopulateListWithEnumValues(enumType);
    }
    
    private void PopulateListWithEnumValues(Type enumType)
    {
        Array enumValues = Enum.GetValues(enumType);
        SerializedProperty listProperty = serializedObject.FindProperty("enumToValueList");

        listProperty.ClearArray();

        foreach (var enumValue in enumValues)
        {
            var element = AddNewArrayElement(listProperty);
            var enumProp = element.FindPropertyRelative("TEnumValue");
            enumProp.enumValueIndex = (int)enumValue;
        }
        serializedObject.ApplyModifiedProperties();
    }

    private SerializedProperty AddNewArrayElement(SerializedProperty listProperty)
    {
        listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
        return listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
    }
}
#endif