﻿//Taken from https://answers.unity.com/questions/242794/inspector-field-for-scene-asset.html

using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SceneField {
    [SerializeField] private Object m_SceneAsset;

    [SerializeField] private string m_SceneName = "";

   
    public string SceneName => m_SceneName;

    // makes it work with the existing Unity methods (LoadLevel/LoadScene)
    public static implicit operator string(SceneField sceneField) {
        return sceneField.SceneName;
    }

}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SceneField))]
public class SceneFieldPropertyDrawer : PropertyDrawer {
    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label) {
        EditorGUI.BeginProperty(_position, GUIContent.none, _property);
        var sceneAsset = _property.FindPropertyRelative("m_SceneAsset");
        var sceneName = _property.FindPropertyRelative("m_SceneName");
        _position = EditorGUI.PrefixLabel(_position, GUIUtility.GetControlID(FocusType.Passive), _label);
        if (sceneAsset != null) {
            sceneAsset.objectReferenceValue =
                EditorGUI.ObjectField(_position, sceneAsset.objectReferenceValue, typeof(SceneAsset), false);
            if (sceneAsset.objectReferenceValue != null)
                sceneName.stringValue = (sceneAsset.objectReferenceValue as SceneAsset).name;
        }

        EditorGUI.EndProperty();
    }
}
#endif