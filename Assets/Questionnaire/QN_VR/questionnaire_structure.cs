using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

// TODO this would be great for the language selection;
//https://stackoverflow.com/a/22912864
//https://stackoverflow.com/questions/34021338/json-net-serializing-the-class-name-instead-of-the-internal-properties
/*
public struct LanguageSelect
{
    private string _value;

    public LanguageSelect(string value)
    {
        this._value = value;
    }

    public static implicit operator string(LanguageSelect l)
    {
        return l._value;
    }

    public static implicit operator LanguageSelect(string l)
    {
        return new LanguageSelect(l);
    }

    public override string ToString()
    {
        return _value;
    }
}
*/
//From https://answers.unity.com/questions/1034235/how-to-write-text-from-left-to-right.html
class StringExtension
{
    public static string Reverse(string s){
        if (s.All(char.IsDigit)){
            return s;
        }
        else{
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
    }

    public static string RTLText(string sin, int numberOfAlphabetsInSingleLine = 50){
        string outstring = "";
        string linestring = "";
        foreach (string s in sin.Split(' ')){
            if (s.Length + linestring.Length > numberOfAlphabetsInSingleLine){
                outstring = outstring + '\n' + '\r' + linestring;
                linestring = "";
            }

            linestring = Reverse(s) + ' ' + linestring; //determin if a number is involved??
        }

        if (linestring.Length > 0){
            outstring = outstring + '\n' + '\r' + linestring;
        }


        return outstring.Trim();
    }

    //https://answers.unity.com/questions/1034235/how-to-write-text-from-left-to-right.html
}


//This object represent multiply answers for a question with the option to define a queue for the next questions in line

public class ObjAnswer
{
    public int index{ get; set; } //This property define the order of the answers
    public Dictionary<string, string> AnswerText{ get; set; }
    public bool FinishQN{ get; set; }
}

public class QuestionnaireQuestion
{
    public int ID{ get; set; } //This will be a unique id for each question in the collection  
    public string Scenario_ID{ get; set; }
    public string SA_atoms{ get; set; } //This property can be used to filter a group of questions
    public string SA_Level{ get; set; } //The level of the question based on SAGAT model
    public string Awareness_to{ get; set; }

    public List<Char> Participant{ get; set; }
    public Dictionary<string, string> QuestionText{ get; set; }
    public string QnImagePath{ get; set; }
    public List<ObjAnswer> Answers{ get; set; }


    private List<ParticipantOrder> AllParticipents;

    private int InteralTrackingID;

    public int getInteralID(){
        return InteralTrackingID;
    }

    public void setInternalID(int inval){
        InteralTrackingID = inval;
    }

    public string GetQuestionText(string lang){
        if (QuestionText.ContainsKey(lang)){
            return QuestionText[lang];
        }

        Debug.Log("Did not find a question for language: " + lang);
        return "";
    }

    public override string ToString(){
        return "ID:" + ID + " ENGQ:" + GetQuestionText("English") + "With answer count:" + Answers.Count.ToString();
    }

    public bool ContainsOrder(ParticipantOrder po){
        if (!initComplete){
            init();
        }

        return AllParticipents.Contains(po);
    }


    private bool initComplete = false;

    public void init(){
        AllParticipents = new List<ParticipantOrder>();
        foreach (char c in Participant){
            switch (c){
                case 'A':
                case 'a':
                    AllParticipents.Add(ParticipantOrder.A);
                    break;
                case 'B':
                case 'b':
                    AllParticipents.Add(ParticipantOrder.B);
                    break;
                case 'C':
                case 'c':
                    AllParticipents.Add(ParticipantOrder.C);
                    break;
                case 'D':
                case 'd':
                    AllParticipents.Add(ParticipantOrder.D);
                    break;
                case 'E':
                case 'e':
                    AllParticipents.Add(ParticipantOrder.E);
                    break;
                case 'F':
                case 'f':
                    AllParticipents.Add(ParticipantOrder.F);
                    break;
            }
        }

        initComplete = true;
    }

    public NetworkedQuestionnaireQuestion GenerateNetworkVersion(string lang){
        NetworkedQuestionnaireQuestion outVal = NetworkedQuestionnaireQuestion.GetDefaultNQQ();
        foreach (ObjAnswer a in Answers){
            if (a.AnswerText.Keys.Contains(lang)){
                outVal.Answers.Add(a.index, a.AnswerText[lang]);
            }
            else{
                Debug.Log(
                    "Did not find Answer for requested language. please fix Data and results will be incomplete!: " +
                    lang + " id:" + a.index.ToString());
            }
        }

        if (QuestionText.Keys.Contains(lang)){
            outVal.QuestionText = QuestionText[lang];
        }
        else{
            Debug.Log(
                "Did not find QuestionText for requested language. please fix Data and results will be incomplete!: " +
                lang + " id:" + ID);
        }

        outVal.ID = getInteralID();
        outVal.reply = replyType.NEWQUESTION;
        QnImagePath ??= "";
        outVal.QnImagePath = QnImagePath;
        Debug.Log(outVal);
        return outVal;
    }
}

public enum replyType : byte
{
    NEWQUESTION,
    FINISHED
}


public struct NetworkedQuestionnaireQuestion : INetworkSerializable
{
    public int ID;
    public string QuestionText;
    public replyType reply;
    public Dictionary<int, string> Answers;
    public string QnImagePath;

    public NetworkedQuestionnaireQuestion(int ID_, replyType reply_, string QuestionText_, string QnImagePath_){
        ID = ID_;
        reply = reply_;
        QuestionText = QuestionText_;
        Answers = new Dictionary<int, string>();
        QnImagePath = QnImagePath_;
    }

    //Factory //
    public static NetworkedQuestionnaireQuestion GetDefaultNQQ(){
        return new NetworkedQuestionnaireQuestion(-1, replyType.FINISHED, "","");
    }


    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
        serializer.SerializeValue(ref ID);
        serializer.SerializeValue(ref QuestionText);
        serializer.SerializeValue(ref reply);
        serializer.SerializeValue(ref QnImagePath);

        int length = 0;

        if (!serializer.IsReader){
            length = Answers.Count;
        }

        serializer.SerializeValue(ref length);

        var qIDs = new int[length];
        var answers = new string[length];


        if (!serializer.IsReader){
            int count = 0;
            foreach (KeyValuePair<int, string> pair in Answers){
                qIDs[count] = pair.Key;
                answers[count] = pair.Value;
                count++;
            }
        }

        for (int n = 0; n < length; ++n){
            serializer.SerializeValue(ref qIDs[n]);
            serializer.SerializeValue(ref answers[n]);
        }

        if (serializer.IsReader){
            Answers = new Dictionary<int, string>();
            for (int n = 0; n < length; ++n){
                Answers.Add(qIDs[n], answers[n]);
            }
        }
    }
}