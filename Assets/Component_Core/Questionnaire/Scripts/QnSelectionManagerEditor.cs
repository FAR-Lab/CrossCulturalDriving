
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


#if UNITY_EDITOR
[CustomEditor(typeof(QN_Display))]
public class QnSelectionManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Render Textreversed"))
        {
            
            QN_Display sm = (QN_Display)target;
            
            sm.transform.Find("QuestionField").GetComponent<Text>().text=StringExtension.RTLText("42 42 42 42 a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a a 42a");
            
        }
        
    }
}
#endif