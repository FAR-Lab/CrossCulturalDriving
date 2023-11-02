using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PushableQNButton : MonoBehaviour {
    public Button m_button;

    public Text m_TextObject;
    
    public string _Answer;
    private string _SecretCharacter;
    private int AnswerIndex;
    
    // Start is called before the first frame update
    void Start() {
     
    }

    public delegate void FinishedCallBack(int AnswerIndex);

    private FinishedCallBack m_callback;
    public void initButton(string Answer, int AnswerIndex_, FinishedCallBack _callback) {
        _Answer = Answer;
        AnswerIndex = AnswerIndex_;
        m_TextObject.text = _Answer;
        m_callback = _callback;
        m_button.onClick.AddListener(()=>m_callback(AnswerIndex_));
    }

    
    // Update is called once per frame
    void Update()
    {
        
    }
}
