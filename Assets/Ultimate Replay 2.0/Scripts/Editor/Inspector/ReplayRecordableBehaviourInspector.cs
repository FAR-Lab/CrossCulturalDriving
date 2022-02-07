using System;
using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayRecordableBehaviour), true)]
    public class ReplayRecordableBehaviourInspector : Editor
    {
        // Protected
        protected ReplayRecordableBehaviour targetBehaviour = null;
        protected ReplayRecordableBehaviour[] targetBehaviours = null;

        // Methods
        public virtual void OnEnable()
        {
            GetTargetInstances(out targetBehaviour, out targetBehaviours);
        }

        public override void OnInspectorGUI()
        {
            // Display main properties
            DisplayDefaultInspectorProperties();

            // Draw data statistics
            DisplayReplayStorageStatistics();
        }

        public void DisplayDefaultInspectorProperties()
        {
            base.OnInspectorGUI();
        }

        protected void DisplayReplayStorageStatistics()
        {
            if (targetBehaviours.Length > 1)
            {
                ReplayStorageStats.DisplayStorageStats(targetBehaviours);
            }
            else
            {
                ReplayStorageStats.DisplayStorageStats(targetBehaviour);
            }
        }

        protected void DisplayMultiEditableToggleField(string label, string tooltip, ref bool[] toggleValues)
        {
            int selected = 0;
            int total = toggleValues.Length;

            for(int i = 0; i < toggleValues.Length; i++)
            {
                if(toggleValues[i] == true)
                {
                    selected++;
                }
            }
            
            GUILayout.BeginHorizontal();
            {
                if (tooltip == null)
                {
                    // Main label
                    GUILayout.Label(label, GUILayout.Width(EditorGUIUtility.labelWidth));
                }
                else
                {
                    GUILayout.Label(new GUIContent(label, tooltip), GUILayout.Width(EditorGUIUtility.labelWidth));
                }
                
                bool isChecked = (total > 0 && total == selected);
                bool isMixed = (selected > 0 && total != selected);

                EditorGUI.showMixedValue = isMixed;

                // Toggle field
                bool isOn = EditorGUILayout.Toggle(isChecked);

                EditorGUI.showMixedValue = false;

                if (isOn != isChecked)
                {
                    // Update array
                    for(int i = 0; i < toggleValues.Length; i++)
                    {
                        toggleValues[i] = isOn;
                    }
                }

                
            }
            GUILayout.EndHorizontal();
        }

        protected void DisplayMultiEditableToggleOnly(ref bool[] toggleValues, params GUILayoutOption[] options)
        {
            int selected = 0;
            int total = toggleValues.Length;

            for (int i = 0; i < toggleValues.Length; i++)
            {
                if (toggleValues[i] == true)
                {
                    selected++;
                }
            }

            bool isChecked = (total > 0 && total == selected);
            bool isMixed = (selected > 0 && total != selected);

            EditorGUI.showMixedValue = isMixed;

            // Toggle field
            bool isOn = EditorGUILayout.Toggle(isChecked, options);

            EditorGUI.showMixedValue = false;

            if (isOn != isChecked)
            {
                // Update array
                for (int i = 0; i < toggleValues.Length; i++)
                {
                    toggleValues[i] = isOn;
                }
            }
        }

        protected void GetTargetInstances<T>(out T singleInstance, out T[] multiInstance) where T : MonoBehaviour
        {
            singleInstance = target as T;
            multiInstance = Array.ConvertAll(targets, i => (T)i);
        }
    }
}
