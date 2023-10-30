using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScenarioManager))]
public class ScenarioManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        if (GUILayout.Button("Attemp QN Deserialization")) {
            var sm = (ScenarioManager)target;
            if (sm != null) {
                var numbers = new List<int>();
                var s = sm.GetQuestionObject();
                foreach (var q in s) {
                    if (numbers.Contains(q.getInteralID()))
                        Debug.LogWarning("Duplicat ID for " + q.getInteralID());
                    else
                        numbers.Add(q.getInteralID());
                   
                    Debug.Log($"{q.QuestionText["English"]}, po:{q.ContainsOrder(ParticipantOrder.B)}");
                    var numbersA = new List<int>();
                    foreach (var a in q.Answers)
                        if (numbersA.Contains(a.index))
                            Debug.LogWarning("Duplicat question index " + a.index + " at question:" + q.getInteralID());
                        else
                            numbersA.Add(a.index);
                }
            }
        }
    }
}