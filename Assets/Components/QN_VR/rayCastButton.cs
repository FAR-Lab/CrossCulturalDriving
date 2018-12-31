using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class rayCastButton : MonoBehaviour {

    // Use this for initialization
   public  string _Answer;
    string _SecretCharacter;
    List<int> _targetIndexes;
    public Text txObj;
    void Awake() {
        txObj = transform.parent.GetComponentInParent<Text>();
    }

    public void initButton(string Answer,List <int> TargetIndex) {
        _Answer = Answer;

        _targetIndexes = TargetIndex;
        txObj.text = _Answer;
    }
    // Update is called once per frame
    void Update() {
        
    }
    public string activateNextQuestions(out List<int> nextID) {
        nextID = _targetIndexes;
        return txObj.text;
    }
}