using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PushableQNButton : MonoBehaviour {
    public Button m_button;

    public TMP_Text m_TextObject;

    public Image ImageTarget;
    
    public string _Answer;
    private string _SecretCharacter;
    private int AnswerIndex;
    
    [Header("TextColorBlock")]
    public ColorBlock TextColorBlock;
    [Header("ImageColorBlock")]
    public ColorBlock ImageColorBlock;

    
    // Start is called before the first frame update
    void Start() {
     
    }

    public delegate void FinishedCallBack(int AnswerIndex);

    private FinishedCallBack m_callback;
    public void initButton(string Answer, int AnswerIndex_, FinishedCallBack _callback, string AnswerImage) {
        _Answer = Answer;
        Sprite _AnswerImageSprite = Resources.Load<Sprite>(AnswerImage);
        Debug.Log($"sprite is {_AnswerImageSprite}");
        ImageTarget.sprite = _AnswerImageSprite;
        AnswerIndex = AnswerIndex_;
        m_TextObject.text = _Answer;
        m_callback = _callback;
        m_button.onClick.AddListener(()=>m_callback(AnswerIndex_));

        if (Answer != "") {
            TextColorBlock.colorMultiplier = 1;
            m_button.colors = TextColorBlock;
        }
        else if (AnswerImage != "") {
            ImageColorBlock.colorMultiplier = 1;
            m_button.colors = ImageColorBlock;
        }
    }

    
    // Update is called once per frame
    void Update()
    {
        
    }
}
