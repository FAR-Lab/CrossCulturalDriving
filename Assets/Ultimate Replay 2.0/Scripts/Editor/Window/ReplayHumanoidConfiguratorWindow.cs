using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;

namespace UltimateReplay
{
    public class ReplayHumanoidConfiguratorWindow : EditorWindow
    {
        // Types
        private class BoneInfo
        {
            // Public
            public HumanBodyBones bone;
            public Transform boneTransform;
            public bool applyToBone = true;
        }

        // Private
        private GUIStyle componentStyle = null;
        private Vector2 scroll = Vector2.zero;
        private ReplayTransform rootTransform = null;
        private ReplayTransform boneTransform = null;
        private bool applyRootTransform = true;
        private BoneInfo[] bones = null;
        private ReplayObject[] existingReplayObejcts = null;
        private ReplayBehaviour[] existingReplayComponents = null;
        private int boneTransformCount = 0;

        private Editor rootTransformEditor = null;
        private Editor boneTransformEditor = null;
        private bool componentsExpanded = false;
        private bool rootTransformEditorExpanded = false;
        private bool boneTransformEditorExpanded = false;
        private bool bonesExpanded = false;
        private int selectionReplayComponentCount = 0;

        // Methods
        public static void ShowWindow()
        {
            GetWindow<ReplayHumanoidConfiguratorWindow>();
        }

        public void OnEnable()
        {
            GameObject temp0 = new GameObject("ReplayPlaceholder_RootTransform");
            GameObject temp1 = new GameObject("ReplayPlaceholder_BoneTransform");
            temp0.hideFlags = HideFlags.HideAndDontSave;
            temp1.hideFlags = HideFlags.HideAndDontSave;

            rootTransform = temp0.AddComponent<ReplayTransform>();
            boneTransform = temp1.AddComponent<ReplayTransform>();
            boneTransform.positionFlags |= ReplayTransform.ReplayTransformFlags.Local;
            boneTransform.rotationFlags |= ReplayTransform.ReplayTransformFlags.Local;
            
            // Build bone list
            RefreshSelection();

            Selection.selectionChanged += OnSelectionChanged;
        }

        public void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;

            if (rootTransform != null)
            {
                DestroyImmediate(rootTransform.gameObject);
                rootTransform = null;
            }

            if(boneTransform != null)
            {
                DestroyImmediate(boneTransform.gameObject);
                boneTransform = null;
            }

            DestroyImmediate(rootTransformEditor);
            DestroyImmediate(boneTransformEditor);
        }

        public void OnGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);
            {
                GUILayout.Space(16);
                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Replay Humanoid Configurator", EditorStyles.largeLabel);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(16);


                // Check for selection
                if (Selection.activeGameObject == null)
                {
                    EditorGUILayout.HelpBox("Select an animated object to start", MessageType.Info);
                    GUILayout.EndScrollView();
                    return;
                }

                if (Selection.activeGameObject.GetComponent<Animator>() == false)
                {
                    EditorGUILayout.HelpBox("The selected object does not have an Animator component attached!", MessageType.Warning);
                    GUILayout.EndScrollView();
                    return;
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Target Object:", GUILayout.Width(EditorGUIUtility.labelWidth));

                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(Selection.activeGameObject, typeof(GameObject), true);
                    EditorGUI.EndDisabledGroup();

                }
                GUILayout.EndHorizontal();

                componentsExpanded = EditorGUILayout.Foldout(componentsExpanded, string.Format("Replay Components ({0})", selectionReplayComponentCount));

                if (componentsExpanded == true)
                {
                    if (existingReplayObejcts != null)
                    {
                        GUILayout.Space(-6);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Space(20);

                            GUILayout.BeginVertical();
                            {
                                Action<Behaviour> displayComponentWithDepth = (Behaviour behaviour) =>
                                {
                                    if (componentStyle == null)
                                    {
                                        componentStyle = new GUIStyle(EditorStyles.helpBox);
                                        componentStyle.active.background = null;
                                        componentStyle.normal.background = null;
                                        componentStyle.wordWrap = false;
                                    }

                                    int relativeDepth = GetObservedComponentRelativeDepth(Selection.activeGameObject.transform, behaviour.transform);

                                    string objectPrefix = string.Empty;

                                    if (relativeDepth == -1) objectPrefix = "?";
                                    else if (relativeDepth > 0) objectPrefix = new string('-', relativeDepth);

                                // Generate name string
                                string objectName = behaviour.gameObject.name;

                                    if (Selection.activeGameObject == behaviour.gameObject) objectName = "<Root>";

                                    GUILayout.Label(string.Format("{0}{1} ({2})", objectPrefix, objectName, behaviour.GetType().Name), componentStyle);

                                    GUILayout.Space(-6);
                                };

                                foreach (ReplayObject obj in existingReplayObejcts)
                                {
                                    displayComponentWithDepth(obj);
                                }

                                foreach (ReplayBehaviour component in existingReplayComponents)
                                {
                                    displayComponentWithDepth(component);
                                }

                                GUILayout.Space(6);
                            }
                            GUILayout.EndVertical();
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                
                rootTransform.noManagingObject = true;
                boneTransform.noManagingObject = true;

                // Create the editor
                Editor.CreateCachedEditor(rootTransform, typeof(ReplayTransformInspector), ref rootTransformEditor);
                Editor.CreateCachedEditor(boneTransform, typeof(ReplayTransformInspector), ref boneTransformEditor);


                // Draw editors
                rootTransformEditorExpanded = EditorGUILayout.Foldout(rootTransformEditorExpanded, "Root Transform");

                if (rootTransformEditorExpanded == true)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.BeginVertical();
                        {
                            (rootTransformEditor as ReplayTransformInspector).DisplayPositionRotationScale();

                            // Display a help message
                            EditorGUILayout.HelpBox(string.Format("This transform setup will be applied to the root object '{0}'", Selection.activeGameObject.name), MessageType.Info);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }

                boneTransformEditorExpanded = EditorGUILayout.Foldout(boneTransformEditorExpanded, "Bone Transforms");

                if (boneTransformEditorExpanded == true)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.BeginVertical();
                        {
                            (boneTransformEditor as ReplayTransformInspector).DisplayPositionRotationScale();

                            // Display a help message
                            EditorGUILayout.HelpBox("This transform setup will be applied to all bones in the hierarchy. It is recommended that local values are recorded for best results.", MessageType.Info);
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }


                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Apply Root Transform:", GUILayout.Width(EditorGUIUtility.labelWidth));
                    applyRootTransform = GUILayout.Toggle(applyRootTransform, GUIContent.none);
                }
                GUILayout.EndHorizontal();

                // Display bones collection
                bonesExpanded = EditorGUILayout.Foldout(bonesExpanded, "Apply Bone Transforms");

                if (bonesExpanded == true)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);
                        GUILayout.BeginVertical();
                        {
                            // Display each bone
                            foreach (BoneInfo bone in bones)
                            {
                                // Check for null bone
                                if (bone == null)
                                    continue;

                                GUILayout.BeginHorizontal();
                                {
                                    // Bone type
                                    GUILayout.Label(bone.bone.ToString(), GUILayout.Width(EditorGUIUtility.labelWidth));

                                    // Bone transform
                                    EditorGUI.BeginDisabledGroup(true);
                                    EditorGUILayout.ObjectField(bone.boneTransform, typeof(Transform), true);
                                    EditorGUI.EndDisabledGroup();

                                    // Active
                                    bone.applyToBone = EditorGUILayout.Toggle(bone.applyToBone, GUILayout.Width(16));
                                }
                                GUILayout.EndHorizontal();
                            }
                        }
                        GUILayout.EndVertical();
                    }
                    GUILayout.EndHorizontal();
                }

                
                // Buttons
                GUILayout.FlexibleSpace();

                // Warning hint
                if (boneTransformCount == 0)
                    EditorGUILayout.HelpBox("The Animator component does not have any bones assigned! Check that the avatar is assigned and setup correctly as a humanoid", MessageType.Warning);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginDisabledGroup(selectionReplayComponentCount == 0);
                    if(GUILayout.Button("Strip Components", GUILayout.Height(30)) == true)
                    {
                        Undo.RegisterCompleteObjectUndo(Selection.activeGameObject, "Strip Replay Components");

                        foreach (ReplayBehaviour component in Selection.activeGameObject.GetComponentsInChildren<ReplayBehaviour>())
                            DestroyImmediate(component);

                        foreach (ReplayObject obj in Selection.activeGameObject.GetComponentsInChildren<ReplayObject>())
                            DestroyImmediate(obj);

                        // Update changes
                        RefreshSelection();
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(boneTransformCount == 0);
                    if(GUILayout.Button("Apply Components", GUILayout.Height(30)) == true)
                    {
                        Undo.RegisterCompleteObjectUndo(Selection.activeGameObject, "Apply Replay Components");

                        // Create the bone configurations
                        ReplayHumanoidConfigurator.ReplayTransformConfiguration rootConfig = ReplayHumanoidConfigurator.ReplayTransformConfiguration.FromReplayTransform(rootTransform, applyRootTransform);
                        ReplayHumanoidConfigurator.ReplayTransformConfiguration boneConfig = ReplayHumanoidConfigurator.ReplayTransformConfiguration.FromReplayTransform(boneTransform, true);

                        // Create the apply bone array
                        List<HumanBodyBones> applyBones = new List<HumanBodyBones>();

                        foreach(BoneInfo bone in bones)
                        {
                            // Check for null bone
                            if (bone == null)
                                continue;

                            // Add to the apply list if the bone should be applied and the transform is assigned
                            if (bone.applyToBone == true && bone.boneTransform != null)
                                applyBones.Add(bone.bone);
                        }

                        // Apply configuration to object
                        ReplayHumanoidConfigurator.ReplayHumanoidConfigurationResult result = ReplayHumanoidConfigurator.ConfigureHumanoidObjectFromAnimator(Selection.activeGameObject, rootConfig, boneConfig, applyBones.ToArray());


                        // Update the selected object with changes
                        RefreshSelection();
                    }
                    EditorGUI.EndDisabledGroup();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(16);
            }
            GUILayout.EndScrollView();
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                ReplayTransform root = Selection.activeGameObject.GetComponent<ReplayTransform>();

                if (root != null)
                {
                    CopyTransformFlags(root, rootTransform);

                    ReplayTransform[] transforms = Selection.activeGameObject.GetComponentsInChildren<ReplayTransform>();

                    // Find first child bone
                    ReplayTransform bone = (transforms.Length > 0) ? transforms.FirstOrDefault(t => t != root) : null;

                    if (bone != null)
                    {
                        // Load transform info for bone
                        CopyTransformFlags(bone, boneTransform);
                    }
                }
                else
                {
                    // Try to find suitable transform which must be attached to a bone
                    ReplayTransform bone = Selection.activeGameObject.GetComponentInChildren<ReplayTransform>();

                    if (bone != null)
                    {
                        // Load transform info for bone
                        CopyTransformFlags(bone, boneTransform);
                    }
                }
            }

            // Build bone list
            RefreshSelection();

            Repaint();
        }

        private void CopyTransformFlags(ReplayTransform fromTransform, ReplayTransform toTransform)
        {
            toTransform.positionFlags = fromTransform.positionFlags;
            toTransform.rotationFlags = fromTransform.rotationFlags;
            toTransform.scaleFlags = fromTransform.scaleFlags;
        }
        
        private void RefreshSelection()
        {
            bones = null;
            boneTransformCount = 0;
            selectionReplayComponentCount = 0;
            existingReplayObejcts = null;
            existingReplayComponents = null;

            if(Selection.activeGameObject != null)
            {
                existingReplayObejcts = Selection.activeGameObject.GetComponentsInChildren<ReplayObject>();
                existingReplayComponents = Selection.activeGameObject.GetComponentsInChildren<ReplayBehaviour>();

                // Get the number of componets
                selectionReplayComponentCount = existingReplayObejcts.Length;
                selectionReplayComponentCount += existingReplayComponents.Length;


                // Get the animator
                Animator anim = Selection.activeGameObject.GetComponent<Animator>();

                if (anim == null)
                    return;

                // Create the array
                bones = new BoneInfo[Enum.GetValues(typeof(HumanBodyBones)).Length];

                // Process each bone
                int index = 0;

                foreach(HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
                {
                    // 'LastBone' is used as a enum length value
                    if (bone >= HumanBodyBones.LastBone)
                        continue;

                    Transform boneTransform = anim.GetBoneTransform(bone);

                    // Create the bone info
                    bones[index] = new BoneInfo
                    {
                        bone = bone,
                        boneTransform = boneTransform,
                        applyToBone = (selectionReplayComponentCount == 0) ? true : boneTransform != null,
                    };

                    // Update counter
                    if (bones[index].boneTransform != null)
                        boneTransformCount++;

                    // Increment index
                    index++;
                }
            }
        }

        private int GetObservedComponentRelativeDepth(Transform root, Transform component)
        {
            int counter = 0;

            while (component != null)
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
    }
}
