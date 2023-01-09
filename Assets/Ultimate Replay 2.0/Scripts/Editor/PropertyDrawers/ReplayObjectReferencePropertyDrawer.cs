using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [CustomPropertyDrawer(typeof(ReplayObject.ReplayObjectReference))]
    public class ReplayObjectReferencePropertyDrawer : PropertyDrawer
    {
        // Methods
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Display the main label
            Rect contentRect = EditorGUI.PrefixLabel(position, label);

            // Find the reference property
            SerializedProperty refProperty = property.FindPropertyRelative("reference");

            // Check for error
            if(refProperty == null)
            {
                GUI.TextField(contentRect, "<error: property 'reference' not found>");
                return;
            }

            string displayString = "<None>";

            // Display the field
            ReplayObject obj = refProperty.objectReferenceValue as ReplayObject;

            if(obj != null)
            {
                displayString = obj.ReplayIdentity.IDString + string.Format(" ({0})", obj.gameObject.name);
            }

            if (property.serializedObject.isEditingMultipleObjects == false)
            {
                // Display the replay identity
                GUI.TextField(contentRect, displayString, EditorStyles.helpBox);
            }
            else
            {
                GUI.TextField(contentRect, char.ConvertFromUtf32(0x00002015), EditorStyles.helpBox);
            }
        }
    }
}
