using System.Collections;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QNDataStorageServer))]
public class QnDataStorageEditor : Editor {
    private IEnumerator writtingCorutine;


    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (GUILayout.Button("Does Nothing!")) {
            var path = EditorUtility.OpenFilePanel("LoadFileFor conversion", Application.persistentDataPath, "replay");
            if (path.Length != 0) {
                // QNDataStorageServer.ConvertToCSV(path);
                //  EditorCoroutineUtility.StartCoroutine(QNDataStorageServer.ConvertToCSV(path),this);
            }
        }
    }
}