using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UltimateReplay.Core;

namespace UltimateReplay
{
    [CustomPropertyDrawer(typeof(ReplayIdentity))]
    public class ReplayIdentityPropertyDrawer : PropertyDrawer
    {
        // Public
        public const int buttonWidth = 0;

        // Methods
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Display the main label
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            // Find the ID property
            SerializedProperty idProperty = property.FindPropertyRelative("id");
            
            // Check for error
            if(idProperty == null)
            {
                GUI.TextField(contentRect, "<error: property 'id' not found>");
                return;
            }

            Rect fieldRect = new Rect(contentRect.x, contentRect.y, contentRect.width - buttonWidth, contentRect.height);
            //Rect buttonRect = new Rect(contentRect.x + contentRect.width - buttonWidth, contentRect.y, buttonWidth, contentRect.height);

            if (property.serializedObject.isEditingMultipleObjects == false)
            {
                // Display the id field
                GUI.TextField(fieldRect, idProperty.intValue.ToString(), EditorStyles.helpBox);
            }
            else
            {
                GUI.TextField(fieldRect, char.ConvertFromUtf32(0x00002015), EditorStyles.helpBox);
            }

            //if(GUI.Button(buttonRect, "Generate") == true)
            //{
            //    property.
            //}
        }
    }
}
