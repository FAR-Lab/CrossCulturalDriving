using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Oculus.Platform;
using Rerun;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class QNDataStorageServer : MonoBehaviour
{
    public static string LogLanguage = "English";
    public static int StartingID = 1;

    private Dictionary<int, QuestionnaireQuestion> activeQuestionList;

    private Dictionary<ParticipantOrder, int> participantAnswerStatus;
    private Dictionary<ParticipantOrder, DateTime> LastParticipantStartTimes;

    private ScenarioLog CurrentScenarioLog;

    // Start is called before the first frame update
    void Start()
    {
        activeQuestionList = new Dictionary<int, QuestionnaireQuestion>();
    }


    public void StartScenario(string name, string sessionName)
    {
        CurrentScenarioLog = new ScenarioLog(name, sessionName);
    }


    public void StartQn(ScenarioManager sManager)
    {
        if (sManager == null) return;
        participantAnswerStatus = new Dictionary<ParticipantOrder, int>();
        LastParticipantStartTimes = new Dictionary<ParticipantOrder, DateTime>();
        foreach (ParticipantOrder po in ConnectionAndSpawing.Singleton.GetCurrentlyConnectedClients())
        {
            participantAnswerStatus.Add(po, 0);
            LastParticipantStartTimes.Add(po, new DateTime());
        }

        if (activeQuestionList != null) activeQuestionList.Clear();
        foreach (QuestionnaireQuestion q in sManager.GetQuestionObject())
        {
            activeQuestionList.Add(q.ID, q);
            //Debug.Log("Adding Question:"+q.QuestionText["English"]);
        }
    }

    public void NewDatapointfromClient(ParticipantOrder po, int id, int answerIndex, string lang)
    {
        if (id > -1) //special question ID used to initialze the process
        {
            if (!CurrentScenarioLog.QuestionResults.ContainsKey(id))
            {
                if (!activeQuestionList.ContainsKey(id))
                {
                    Debug.LogError("trying to log a question however question is not present");
                }

                CurrentScenarioLog.QuestionResults.Add(id, new QuestionLog(activeQuestionList[id]));
            }

         

            CurrentScenarioLog.QuestionResults[id].ParticipantsReponse.Add((char) po,
                new QuestionLog.ParticipantsAnswerReponse
                {
                    AnswerId = answerIndex,
                    AnswerText = activeQuestionList[id].Answers.First(s => s.index == answerIndex)
                        .AnswerText[LogLanguage],
                    StartTimeQuestion = LastParticipantStartTimes[po],
                    StopTimeQuestion = DateTime.Now
                });

            Debug.Log("There should be some storage code here: " + po + id.ToString() + "  " + answerIndex.ToString());
        }


        SendNewQuestion(po, lang);
    }


    public void StopScenario(string filePath)
    {
        CurrentScenarioLog.Stop();

        string fullPath = filePath
                          + "Scenario-" + CurrentScenarioLog.ScenarioName + '_'
                          + "sessionName-" + CurrentScenarioLog.participantComboName + '_'
                          + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json";
        string s = JsonConvert.SerializeObject(CurrentScenarioLog);

        System.IO.File.WriteAllText(fullPath, s);
    }

    private void SendNewQuestion(ParticipantOrder p, string lang)
    {
        int val = participantAnswerStatus[p];
        val++;


        while (activeQuestionList.ContainsKey(val) && !activeQuestionList[val].ContainsOrder(p))
        {
            val++;
            Debug.Log("looking for a next relevant question!" + val.ToString());
        }

        NetworkedQuestionnaireQuestion outval;
        if (!activeQuestionList.ContainsKey(val))
        {
            outval = NetworkedQuestionnaireQuestion.GetDefaultNQQ();
            Debug.Log("Finished Questionnaire sending a finish message!!  " + val.ToString());
        }
        else
        {
            participantAnswerStatus[p] = val;
            outval = activeQuestionList[val].GenerateNetworkVersion(lang);
            Debug.Log("Found Another question to send " + val.ToString());
            LastParticipantStartTimes[p]=DateTime.Now;
        }

        ConnectionAndSpawing.Singleton.SendNewQuestionToParticipant(p, outval);
    }


/*
    private void DataStorage(ulong senderclientid, FastBufferReader messagepayload)
    {
        if (_rerunManager == null)
        {
            Debug.Log(messagepayload.Length);
        }
        else
        {
            ParticipantOrder po = ConnectionAndSpawing.Singleton.GetParticipantOrderClientId(senderclientid);
            string fullPath = _rerunManager.GetCurrentFilePath()
                              + "Scenario-" + ConnectionAndSpawing.Singleton.GetLoadedScene() + '_'
                              + "po-" + po.ToString() + '_'
                              + "Session-" + _rerunManager.GetRecordingFolder() + '_'
                              + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".txt";

            messagepayload.ReadNetworkSerializable<LongStringMessage>(out LongStringMessage value);
            System.IO.File.WriteAllText(fullPath, value.message);
        }
    }
*/
}


public struct QuestionLog
{
    public int ID { get; set; }
    
    public string Scenario_ID { get; set; }
    
    public string QuestionText { get; set; }
   
    public string SA_atoms { get; set; } //This property can be used to filter a group of questions
    public string SA_Level { get; set; } //The level of the question based on SAGAT model
    public string Awareness_to { get; set; }

    public Dictionary<char, ParticipantsAnswerReponse> ParticipantsReponse { get; set; }

    public struct ParticipantsAnswerReponse
    {
        public int AnswerId;
        public string AnswerText;
        public DateTime StartTimeQuestion;
        public DateTime StopTimeQuestion;


        public void FinalUpdate(int answerId, string answerText)
        {
            StopTimeQuestion = DateTime.Now;
            AnswerId = answerId;
            AnswerText = answerText;
        }

        public void SetStartTimeQuestion(DateTime now)
        {
            StartTimeQuestion = now;
        }
    }

    public QuestionLog(QuestionnaireQuestion qIn)
    {
        ID = qIn.ID;
        QuestionText = qIn.QuestionText[QNDataStorageServer.LogLanguage];
        Scenario_ID = qIn.Scenario_ID;
        SA_atoms = qIn.SA_atoms;
        SA_Level = qIn.SA_Level;
        Awareness_to = qIn.Awareness_to;
        ParticipantsReponse = new Dictionary<char, ParticipantsAnswerReponse>();
    }
}

public struct ScenarioLog
{
    public DateTime startTime { get; set; } //Timestamp when the scenario started
    public DateTime endTime { get; set; } //Timestamp when the scenario finished and the questionnaire started

    public char
        participantTriggeredQuestionnaire
    {
        get;
        set;
    } //The participant that walked into the invisible box that initiated the questionnaire. Could be more than 1

    public string ScenarioName { get; set; }

    public string
        participantComboName { get; set; } //I didn't use the term pair to have the flexability for more than 2 participant

    public Dictionary<string, string>
        facts
    {
        get;
        set;
    } //Dictionary collection of all objective facts we would like to record next to the question for further examination. 

    public Dictionary<int, QuestionLog> QuestionResults { get; set; }

    public ScenarioLog(string name, string participantComboName_)
    {
        startTime = DateTime.Now;
        endTime = DateTime.MaxValue;
        participantTriggeredQuestionnaire = '-';
        ScenarioName = name;
        participantComboName = participantComboName_;
        facts = new Dictionary<string, string>();
        QuestionResults = new Dictionary<int, QuestionLog>();
    }

    public void Stop()
    {
        endTime = DateTime.Now;
    }
}


/*
public struct LongStringMessage : INetworkSerializable
{
    public string message;


    public int GetSize()
    {
        UnicodeEncoding unicode = new UnicodeEncoding();
        return unicode.GetByteCount(message) + 6; // plus 4 for the int
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        UnicodeEncoding unicode = new UnicodeEncoding();

        int ByteLength = 0;
        byte[] temp;
        if (!serializer.IsReader)
        {
            ByteLength = unicode.GetByteCount(message);
        }

        serializer.SerializeValue(ref ByteLength);

        if (serializer.IsReader)
        {
            temp = new byte[ByteLength];
            //  Debug.Log("I allocated " + ByteLength);
        }
        else
        {
            temp = unicode.GetBytes(message);
            Debug.Log("I send allocated:" + ByteLength + " my array has this many bytes: " + temp.Length);
        }

        for (int n = 0; n < ByteLength; ++n)
        {
            serializer.SerializeValue(ref temp[n]);
        }

        if (serializer.IsReader)
        {
            message = unicode.GetString(temp);
        }
    }
}
*/

/*  bool stateChange = false;
        if (lastActionState != ConnectionAndSpawing.Singleton.ServerState)
        {
            stateChange = true;
            lastActionState = ConnectionAndSpawing.Singleton.ServerState;
        }

        switch (ConnectionAndSpawing.Singleton.ServerState)
        {
            case ActionState.DEFAULT:
                break;
            case ActionState.WAITINGROOM:
                break;
            case ActionState.LOADING:
                break;
            case ActionState.READY:
                break;
            case ActionState.DRIVE:
                break;
            case ActionState.QUESTIONS:

                break;
            case ActionState.POSTQUESTIONS:
                break;
            case ActionState.RERUN:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }*/

/*

LongStringMessage qnmessage = new LongStringMessage();
qnmessage.message = sManager.GetQuestionFile().text;
Debug.Log("The message is this long:" + qnmessage.GetSize());

using FastBufferWriter writer = new FastBufferWriter(qnmessage.GetSize(), Allocator.Temp);
Debug.Log("The writer has this much space:" + writer.Capacity);
writer.WriteNetworkSerializable(qnmessage);
NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(QNDataStorageServer.QNContentMessageName,
    writer, NetworkDelivery.ReliableFragmentedSequenced); //NetworkDelivery is optional.*/