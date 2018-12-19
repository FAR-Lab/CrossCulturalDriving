using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class rayCastButton : MonoBehaviour {

    // Use this for initialization
    string _Answer;
    string _SecretCharacter;
    int _targetIndex;
    Text txObj;
	void Start () {
        txObj= GetComponentInParent<Text>();
    }

    public void initButton(string Answer, string SecretCharacter,int TargetIndex) {
        _Answer = Answer;
        _SecretCharacter = SecretCharacter;
         _targetIndex=TargetIndex;

        txObj.text = _Answer;
    }
    // Update is called once per frame
    void Update () {
		
	}
    public string activateNextQuestions() {

        return txObj.text;
            }
}
