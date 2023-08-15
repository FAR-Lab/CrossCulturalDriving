using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEditor;
using Unity.EditorCoroutines.Editor;
[CustomEditor(typeof(QNDataStorageServer))]
public class QnDataStorageEditor : Editor
{
    private IEnumerator writtingCorutine;
    
   
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Does Nothing!"))
        {
            string path = EditorUtility.OpenFilePanel("LoadFileFor conversion", Application.persistentDataPath, "replay");
            if (path.Length != 0)
            {
               // QNDataStorageServer.ConvertToCSV(path);
                //  EditorCoroutineUtility.StartCoroutine(QNDataStorageServer.ConvertToCSV(path),this);

            }
        }
    }
    
}