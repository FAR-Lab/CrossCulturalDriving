using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class rayCastButton : MonoBehaviour {
    // Use this for initialization
    public string _Answer;
    string _SecretCharacter;
    public Text txObj;
    private int AnswerIndex;
    void Awake() { txObj = transform.parent.GetComponentInParent<Text>(); }

    public void initButton(string Answer, int AnswerIndex_) {
        _Answer = Answer;
        AnswerIndex = AnswerIndex_;
        txObj.text = _Answer;
    }

    public int activateNextQuestions() { return AnswerIndex; }
}