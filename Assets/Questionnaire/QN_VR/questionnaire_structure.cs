using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

// TODO this would be great for the language selection;
//https://stackoverflow.com/a/22912864
//https://stackoverflow.com/questions/34021338/json-net-serializing-the-class-name-instead-of-the-internal-properties

  public struct LanguageSelect {
 
    private string _value;
    public LanguageSelect(string value) {
        this._value = value;
    }
    public static implicit operator string(LanguageSelect l) {
        return l._value;
    }
    public static implicit operator LanguageSelect(string l) {
        return new LanguageSelect(l);
    }

    public override string ToString()
    {
        return _value;
    }
}


    
//This object represent multiply answers for a question with the option to define a queue for the next questions in line

public class ObjAnswer{
    public int index {get; set;} //This property define the order of the answers
    public Dictionary<LanguageSelect,string > AnswerText {get; set;}
    public List<int> nextQuestionIndexQueue {get; set;}
    
}

public class QuestionnaireQuestion{
    public int ID {get; set;} //This will be a unique id for each question in the collection    
    public string Behavior {get; set;} //This property can be used to filter a group of questions
    public string SA_Level {get; set;} //The level of the question based on SAGAT model

    public Dictionary<LanguageSelect,string > QuestionText {get; set;}

    public List<ObjAnswer> Answers {get; set;}

    public string GetQuestionText(string lang)
    {
        
        if (QuestionText.ContainsKey(lang))
        {
           
            return QuestionText[lang];
        }
        Debug.Log("Did not find a question for language: "+lang);
        return "";
    }

    public override string ToString()
    {
        return "ID:" + ID + " ENGQ:" + GetQuestionText("English")+"With answer count:"+Answers.Count.ToString();
    }
}