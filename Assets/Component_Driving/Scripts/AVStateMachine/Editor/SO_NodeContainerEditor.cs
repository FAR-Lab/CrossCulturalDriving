using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SO_NodeContainer))]
public class SO_NodeContainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SO_NodeContainer nodeContainer = (SO_NodeContainer)target;

        if (GUILayout.Button("Open FSM Graph"))
        {
            FSMGraphWindow.ShowWindow(nodeContainer);
        }
    }
}