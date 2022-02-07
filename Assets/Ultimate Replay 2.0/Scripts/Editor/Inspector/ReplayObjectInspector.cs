using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ReplayObject))]
    public class ReplayObjectInspector : ReplayRecordableBehaviourInspector
    {
        // Private
        private ReplayObject targetReplayObject = null;
        private ReplayObject[] targetReplayObjects = null;
        private GUIStyle foldoutStyle = null;
        private GUIStyle componentStyle = null;
        private Texture2D lineTexture = null;

        // Methods
        public override void OnEnable()
        {
            // Dont call base method
        }

        public override void OnInspectorGUI()
        {
            // Get target
            GetTargetInstances(out targetReplayObject, out targetReplayObjects);

            BuildStyles();

            DisplayDefaultInspectorProperties();

            // Display properties
            EditorGUILayout.PropertyField(serializedObject.FindProperty("replayIdentity"));

            //if (targetReplayObjects.Length > 1 || targetReplayObject.IsPrefab == true)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("prefabIdentity"));

            // Observed components
            if (targetReplayObjects.Length > 1)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Replay Components", GUILayout.Width(EditorGUIUtility.labelWidth));
                    GUILayout.Label(char.ConvertFromUtf32(0x00002015));
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                {
                    // Get observed components
                    IList<ReplayRecordableBehaviour> observed = targetReplayObject.ObservedComponents;

                    if (observed.Count == 0)
                    {
                        GUILayout.Label("Replay Components", GUILayout.Width(EditorGUIUtility.labelWidth));
                        GUILayout.Label("(None)");
                    }
                    else
                    {
                        // Display foldout
                        targetReplayObject.isObservedComponentsExpanded = GUILayout.Toggle(targetReplayObject.isObservedComponentsExpanded, "Replay Components", EditorStyles.foldout, GUILayout.Width(EditorGUIUtility.labelWidth - 12));

                        GUIStyle verticalStyle = GUIStyle.none;

                        if (targetReplayObject.isObservedComponentsExpanded == true)
                            verticalStyle = EditorStyles.helpBox;

                        GUILayout.BeginVertical(verticalStyle);
                        {
                            if(targetReplayObject.isObservedComponentsExpanded == false)
                                GUILayout.Label(string.Format("({0})", observed.Count));

                            
                            if (targetReplayObject.isObservedComponentsExpanded == true)
                            {
                                for(int i = 0; i < observed.Count; i++)
                                {
                                    if (observed[i] == null)
                                        continue;

                                    int relativeDepth = GetObservedComponentRelativeDepth(targetReplayObject.transform, observed[i].transform);


                                    // Generate prefix string
                                    string objectPrefix = string.Empty;

                                    if (relativeDepth == -1) objectPrefix = "?";
                                    else if (relativeDepth > 0) objectPrefix = new string('-', relativeDepth);


                                    // Generate name string
                                    string objectName = observed[i].gameObject.name;

                                    if (observed[i].gameObject == targetReplayObject.gameObject) objectName = "<Root>";



                                    GUILayout.Label(string.Format("{0}{1} ({2})", objectPrefix, objectName, observed[i].GetType().Name), componentStyle);

                                    Rect lineRect = GUILayoutUtility.GetLastRect();

                                    if (i < observed.Count - 1)
                                    {
                                        GUILayout.Space(-6);

                                        lineRect.y += EditorGUIUtility.singleLineHeight;
                                        lineRect.height = 1;

                                        EditorGUI.DrawRect(lineRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
                                    }
                                }
                            }

                            
                        }
                        GUILayout.EndVertical();
                    }
                }
                GUILayout.EndHorizontal();
            }


            // Display storage info
            DisplayReplayStorageStatistics();
        }

        private new void DisplayReplayStorageStatistics()
        {
            if (targetReplayObjects.Length > 1)
            {
                ReplayStorageStats.DisplayStorageStats(targetReplayObjects);
            }
            else
            {
                ReplayStorageStats.DisplayStorageStats(targetReplayObject);
            }
        }

        private int GetObservedComponentRelativeDepth(Transform root, Transform component)
        {
            int counter = 0;

            while(component != null)
            {
                if (component == root)
                    return counter;

                // Move up
                component = component.parent;
                counter++;
            }

            // Root was not a parent of component
            return -1;
        }

        private void BuildStyles()
        {
            if(foldoutStyle == null)
            {
                foldoutStyle = new GUIStyle(EditorStyles.foldout);
                foldoutStyle.fixedWidth = EditorGUIUtility.labelWidth;
                foldoutStyle.stretchWidth = false;
            }

            if(componentStyle == null)
            {
                componentStyle = new GUIStyle(EditorStyles.helpBox);
                componentStyle.active.background = null;
                componentStyle.normal.background = null;
                componentStyle.wordWrap = false;
            }

            if(lineTexture == null)
            {
                lineTexture = new Texture2D(1, 1);
                lineTexture.SetPixel(0, 0, new Color(0.2f, 0.2f, 0.2f, 1f));
                lineTexture.Apply();
            }
        }
    }
}
