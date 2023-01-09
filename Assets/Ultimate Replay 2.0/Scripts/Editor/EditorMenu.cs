using UnityEditor;
using UnityEngine;

namespace UltimateReplay
{
    internal static class EditorMenu
    {
        // Methods
        [MenuItem("Tools/Ultimate Replay/Settings")]
        public static void ShowSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(UltimateReplay).FullName);

            if(guids.Length > 0)
            {
                // Get the asset path
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);

                // Load the settings asset
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<UltimateReplay>(path);
            }
        }

        [MenuItem("Tools/Ultimate Replay/Replay Controls", priority = 100)]
        public static void AddReplayControls()
        {
            GameObject go = new GameObject("Replay Controls");

            Undo.SetCurrentGroupName("Add Replay Controls");
            int group = Undo.GetCurrentGroup();

            Undo.RegisterCreatedObjectUndo(go, "Create Replay Controls");

            Undo.AddComponent<ReplayControls>(go);

            Undo.CollapseUndoOperations(group);
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Object", priority = -20)]
        public static void AddReplayObject()
        {
            AddReplayObjectToSelection();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Transform")]
        public static void AddReplayTransform()
        {
            AddReplayComponentToSelection<ReplayTransform>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Enabled State")]
        public static void AddReplayEnabledState()
        {
            AddReplayComponentToSelection<ReplayEnabledState>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Component Enabled State")]
        public static void AddReplayComponentEnabledState()
        {
            AddReplayComponentToSelection<ReplayComponentEnabledState>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Animator")]
        public static void AddReplayAnimator()
        {
            AddReplayComponentToSelection<ReplayAnimator>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Particle System")]
        public static void AddReplayParticleSystem()
        {
            AddReplayComponentToSelection<ReplayParticleSystem>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Audio")]
        public static void AddReplayAudio()
        {
            AddReplayComponentToSelection<ReplayAudio>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Line Renderer")]
        public static void AddReplayLineRenderer()
        {
            AddReplayComponentToSelection<ReplayLineRenderer>();
        }

#if UNITY_2018_2_OR_NEWER
        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Trail Renderer")]
        public static void AddReplayTrailRenderer()
        {
            AddReplayComponentToSelection<ReplayTrailRenderer>();
        }
#endif

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Blend Shape")]
        public static void AddReplayBlendShape()
        {
            AddReplayComponentToSelection<ReplayBlendShape>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Material/Material Change")]
        public static void AddReplayMaterialChange()
        {
            AddReplayComponentToSelection<ReplayMaterialChange>();
        }

        [MenuItem("Tools/Ultimate Replay/Make Selection Replayable/Replay Material/Material Properties")]
        public static void AddReplayMaterialProperties()
        {
            AddReplayComponentToSelection<ReplayMaterial>();
        }

        [MenuItem("Tools/Ultimate Replay/Setup/Replay Humanoid")]
        public static void SetupReplayHumanoid()
        {
            ReplayHumanoidConfiguratorWindow.ShowWindow();
        }

        public static void AddReplayObjectToSelection()
        {
            // Check for no selection
            if (Selection.activeGameObject == null)
                return;

            // Get selected game objects
            GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable);

            if (selected.Length == 0)
                return;

            // Record the apply operation
            Undo.RecordObjects(selected, "Add Replay Object");

            // Process all selected
            foreach (GameObject obj in selected)
            {
                // Check for already existing component
                if (obj.GetComponent<ReplayObject>() == null)
                {
                    // Add the component
                    obj.AddComponent<ReplayObject>();

                    // Record modifications
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            }
        }

        public static void AddReplayComponentToSelection<T>() where T : ReplayRecordableBehaviour
        {
            // Check for no selection
            if (Selection.activeGameObject == null)
                return;

            // Get selected game objects
            GameObject[] selected = Selection.GetFiltered<GameObject>(SelectionMode.Editable);

            if (selected.Length == 0)
                return;

            // Record the apply operation
            Undo.RecordObjects(selected, "Add " + typeof(T).Name);

            // Process all selected
            foreach (GameObject obj in selected)
            {
                // Check for already existing component
                if (obj.GetComponent<T>() == null)
                {
                    // Add the component
                    obj.AddComponent<T>();

                    // Record modifications
                    PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
                }
            }
        }
    }
}
