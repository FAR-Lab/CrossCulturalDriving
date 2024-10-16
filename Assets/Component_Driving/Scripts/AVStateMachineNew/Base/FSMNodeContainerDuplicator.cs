using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FSMNodeContainerDuplicator
{
    [MenuItem("Tools/DuplicateContainer")]
    static void DuplicateContainer()
    {
        SO_FSMNodeContainer selectedContainer = Selection.activeObject as SO_FSMNodeContainer;
        if (selectedContainer == null)
        {
            Debug.LogError("Please select a SO_FSMNodeContainer to duplicate.");
            return;
        }

        string containerPath = AssetDatabase.GetAssetPath(selectedContainer);
        string containerDirectory = Path.GetDirectoryName(containerPath);

        string duplicatedFolderPath = Path.Combine(containerDirectory, "duplicated");
        if (!AssetDatabase.IsValidFolder(duplicatedFolderPath))
        {
            AssetDatabase.CreateFolder(containerDirectory, "duplicated");
        }

        string duplicatedContainerPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(duplicatedFolderPath, selectedContainer.name + ".asset"));
        SO_FSMNodeContainer duplicatedContainer = ScriptableObject.CreateInstance<SO_FSMNodeContainer>();
        AssetDatabase.CreateAsset(duplicatedContainer, duplicatedContainerPath);

        // Initialize dictionaries to keep track of duplicated nodes and transitions
        Dictionary<SO_FSMNode, SO_FSMNode> duplicatedNodes = new Dictionary<SO_FSMNode, SO_FSMNode>();
        Dictionary<SO_FSMTransition, SO_FSMTransition> duplicatedTransitions = new Dictionary<SO_FSMTransition, SO_FSMTransition>();

        // Duplicate the nodes starting from the startNode
        if (selectedContainer.startNode != null)
        {
            SO_FSMNode duplicatedStartNode = DuplicateNodeRecursive(selectedContainer.startNode, duplicatedFolderPath, duplicatedNodes, duplicatedTransitions);
            duplicatedContainer.startNode = duplicatedStartNode;
        }

        EditorUtility.SetDirty(duplicatedContainer);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Duplication completed.");
    }

    static SO_FSMNode DuplicateNodeRecursive(
        SO_FSMNode originalNode,
        string duplicatedFolderPath,
        Dictionary<SO_FSMNode, SO_FSMNode> duplicatedNodes,
        Dictionary<SO_FSMTransition, SO_FSMTransition> duplicatedTransitions)
    {
        if (duplicatedNodes.ContainsKey(originalNode))
        {
            return duplicatedNodes[originalNode];
        }

        // Duplicate the node
        string originalNodePath = AssetDatabase.GetAssetPath(originalNode);
        string nodeName = Path.GetFileNameWithoutExtension(originalNodePath);
        string duplicatedNodePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(duplicatedFolderPath, nodeName + ".asset"));
        SO_FSMNode duplicatedNode = ScriptableObject.CreateInstance<SO_FSMNode>();
        duplicatedNode.name = nodeName;
        AssetDatabase.CreateAsset(duplicatedNode, duplicatedNodePath);

        duplicatedNodes.Add(originalNode, duplicatedNode);

        duplicatedNode.Transitions = new List<SO_FSMTransition>();

        // Save the duplicated node before setting references
        EditorUtility.SetDirty(duplicatedNode);
        AssetDatabase.SaveAssets();

        duplicatedNode.Action = originalNode.Action;

        // Mark the duplicated node as dirty after setting the Action
        EditorUtility.SetDirty(duplicatedNode);
        AssetDatabase.SaveAssets();

        foreach (var originalTransition in originalNode.Transitions)
        {
            SO_FSMTransition duplicatedTransition = null;

            if (duplicatedTransitions.ContainsKey(originalTransition))
            {
                duplicatedTransition = duplicatedTransitions[originalTransition];
            }
            else
            {
                // Duplicate the transition
                duplicatedTransition = ScriptableObject.CreateInstance<SO_FSMTransition>();
                string originalTransitionPath = AssetDatabase.GetAssetPath(originalTransition);
                string transitionName = Path.GetFileNameWithoutExtension(originalTransitionPath);
                string duplicatedTransitionPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(duplicatedFolderPath, transitionName + ".asset"));
                duplicatedTransition.name = transitionName;
                AssetDatabase.CreateAsset(duplicatedTransition, duplicatedTransitionPath);

                duplicatedTransitions.Add(originalTransition, duplicatedTransition);

                EditorUtility.SetDirty(duplicatedTransition);
                AssetDatabase.SaveAssets();

                duplicatedTransition.condition = originalTransition.condition;

                duplicatedTransition.InverseCondition = originalTransition.InverseCondition;

                if (originalTransition.targetNode != null)
                {
                    duplicatedTransition.targetNode = DuplicateNodeRecursive(originalTransition.targetNode, duplicatedFolderPath, duplicatedNodes, duplicatedTransitions);
                }

                EditorUtility.SetDirty(duplicatedTransition);
                AssetDatabase.SaveAssets();
            }

            duplicatedNode.Transitions.Add(duplicatedTransition);
        }

        EditorUtility.SetDirty(duplicatedNode);
        AssetDatabase.SaveAssets();

        return duplicatedNode;
    }
}
