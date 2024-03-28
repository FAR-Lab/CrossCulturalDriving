using UnityEngine;
using UnityEngine.UI;

public class rayCastButton : MonoBehaviour {
    // Use this for initialization
    public string _Answer;
    public Text txObj;
    private string _SecretCharacter;
    private int AnswerIndex;

    private void Awake() {
        txObj = transform.parent.GetComponentInParent<Text>();
    }

    public void initButton(string Answer, int AnswerIndex_) {
        _Answer = Answer;
        AnswerIndex = AnswerIndex_;
        txObj.text = _Answer;
    }

    public int activateNextQuestions() {
        return AnswerIndex;
    }
}