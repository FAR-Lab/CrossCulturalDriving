// #define LOGREPLAYIDS

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Rerun;
using UltimateReplay;
using Unity.Netcode;
using UnityEngine;


public class QNDataStorageServer : MonoBehaviour {
    public static string LogLanguage = "English";
    public static int StartingID = 1;

    private Dictionary<int, QuestionnaireQuestion> activeQuestionList;

    private Dictionary<ParticipantOrder, int> participantAnswerStatus;
    private Dictionary<ParticipantOrder, DateTime> LastParticipantStartTimes;
    private int StartID = -1;
    private ScenarioLog CurrentScenarioLog;


    public static JsonSerializerSettings JsonDateSerelizerSettings = new JsonSerializerSettings
        { DateFormatString = DataStoragePathSupervisor.DateTimeFormatInternal };

    private IEnumerator writtingCorutine;


    private Dictionary<ParticipantOrder, QN_Display> qnDisplays;


    void Start() {
        activeQuestionList = new Dictionary<int, QuestionnaireQuestion>();
        qnDisplays = new Dictionary<ParticipantOrder, QN_Display>();
        //gtLogger = new GroundTruthLogger();
    }

    private void Update() {
        if (ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE) {
            //gtLogger.Update();
        }
    }

    public void StartScenario(string name, string sessionName) {
        CurrentScenarioLog = new ScenarioLog(name, sessionName);
        Debug.Log("Started a new questionnaire");

        //gtLogger.StartScenario(
        // ConnectionAndSpawning.Singleton.GetCurrentlyConnectedClients().ToArray()
        // );
    }


    public void StartQn(ScenarioManager sManager, RerunManager activeManager) {
        if (sManager == null) return;
        //gtLogger.GatherGroundTruth(ref CurrentScenarioLog);
        participantAnswerStatus = new Dictionary<ParticipantOrder, int>();
        LastParticipantStartTimes = new Dictionary<ParticipantOrder, DateTime>();
        ParticipantCountCurrent = new Dictionary<ParticipantOrder, int>();
        bool first = true;

        if (activeQuestionList != null) activeQuestionList.Clear();
        foreach (QuestionnaireQuestion q in sManager.GetQuestionObject()) {
            if (first) {
                first = false;
                StartID = q.getInteralID() - 1; // Start ID to move forward.
            }

            activeQuestionList.Add(q.getInteralID(), q);
        }

        /*
        foreach (var a in activeQuestionList[0].Answers) {
            Debug.Log(a.index);
            foreach (var v in a.AnswerText.Values) {
                Debug.Log(v);
            }
            Debug.Log(a.AnswerImage);
        }
        */


        foreach (ParticipantOrder po in ConnectionAndSpawning.Singleton.GetCurrentlyConnectedClients()) {
            if (po == ParticipantOrder.None) continue;
            participantAnswerStatus.Add(po, StartID);
            LastParticipantStartTimes.Add(po, new DateTime());
            ParticipantCountCurrent.Add(po, 0);
            SendTotalQNCount(po,
                activeQuestionList.Count(x => x.Value.ContainsOrder(po)));
            Debug.Log("Just initiated QN for participant:" + po);
        }

#if LOGREPLAYIDS
        Dictionary<int, LogHeader> ObjectHeader = new Dictionary<int, LogHeader>();
        Dictionary<int, LogHeader> ComponentHeader = new Dictionary<int, LogHeader>();

        foreach (var a in FindObjectsOfType<ParticipantOrderReplayComponent>()) {
            ObjectHeader.Add(a.ReplayObject.ReplayIdentity.IDValue,
                new LogHeader {
                    name = a.ReplayObject.transform.name, logFlag = LogFlag.PARTICIPANTORDER,
                    po = a.GetParticipantOrderAsChar(),
                    m_ReplayObject = a.ReplayObject.ReplayIdentity.IDValue
                });
            Debug.Log(a.ReplayObject.transform.name);
        }


        foreach (var a in FindObjectsOfType<ReplayBehaviour>()) {
            LogFlag lf = LogFlag.POSE;

            if (a.GetType() == typeof(ParticipantOrderReplayComponent)) {
                lf = LogFlag.PARTICIPANTORDER;
            }
            else if (a.GetType() == typeof(ReplayTransform)) {
                lf = LogFlag.POSE;
            }
            else if (a.GetType() == typeof(ReplayMaterialChange)) {
                lf = LogFlag.MATERIALCHANGE;
            }
            else if (a.GetType() == typeof(Speedometer)) {
                lf = LogFlag.SPEEDOMETER;
            }
            else if (a.GetType() == typeof(NavigationScreen)) {
                lf = LogFlag.GPS;
            }
            else {
                Debug.Log("Missing serealization objection for =>" + a.GetType());
            }


            if (LogFlag.PARTICIPANTORDER != lf) {
                if (ObjectHeader.ContainsKey(a.ReplayObject.ReplayIdentity.IDValue)) {
                    if (!ComponentHeader.ContainsKey(a.ReplayIdentity.IDValue)) {
                        ComponentHeader.Add(a.ReplayIdentity.IDValue,
                            new LogHeader {
                                name = a.transform.name,
                                po = ObjectHeader[a.ReplayObject.ReplayIdentity.IDValue].po,
                                logFlag = lf
                            });
                    }
                    else {
                        Debug.Log("Duplicat key:" + a.transform.name + "<  =  >" +
                                  ComponentHeader[a.ReplayIdentity.IDValue].name);
                    }
                }
                else {
                    ComponentHeader.Add(a.ReplayIdentity.IDValue,
                        new LogHeader { name = a.transform.name, logFlag = lf });
                }
            }
        }

        string folderpath = activeManager.GetCurrentFolderPath() + "/MetaData/";
        System.IO.Directory.CreateDirectory(folderpath);
        string fullPath = folderpath
                          + "Serialization_MetaData"
                          + "Scenario-" + CurrentScenarioLog.ScenarioName + '_'
                          + "Session-" + CurrentScenarioLog.participantComboName + '_'
                          + System.DateTime.Now.ToString(DateTimeFormatFolder) + ".json";
        string s = JsonConvert.SerializeObject(
            new LogMetaData { ObjectHeader = ObjectHeader, ComponentHeader = ComponentHeader },
            JsonDateSerelizerSettings
        );

        System.IO.File.WriteAllText(fullPath, s);

#endif

        foreach (var qnDisplays in qnDisplays.Values) {
            qnDisplays.StartQuestionair();
        }
    }


    struct LogMetaData {
        public Dictionary<int, LogHeader> ObjectHeader { get; set; }
        public Dictionary<int, LogHeader> ComponentHeader { get; set; }
    }

    public static string GenerateHeader(LogHeader tmp) {
        return tmp.name + "-" + ((ParticipantOrder)tmp.po).ToString();
    }


    public void NewDatapointfromClient(ParticipantOrder po, int id, int answerIndex, string lang) {
        Debug.Log($"New Data Point from po{po}");
        if (answerIndex == -1 && id == -1) {
            SendPreviousQuestion(po, lang);
            ParticipantCountCurrent[po]--;
            if (ParticipantCountCurrent[po] < 0) {
                ParticipantCountCurrent[po] = 0;
            }

            UpdateTotalParticipantCounts();
            return;
        }

        if (id > -1) //special question ID used to initialze the process
        {
            if (!CurrentScenarioLog.QuestionResults.ContainsKey(id)) {
                if (!activeQuestionList.ContainsKey(id)) {
                    Debug.LogError("Trying to log a question however question is not present");
                }

                CurrentScenarioLog.QuestionResults.Add(id, new QuestionLog(activeQuestionList[id]));
            }

            string AnswerTextTMP = "";
            var tmp = activeQuestionList[id].Answers.First(s => s.index == answerIndex);
            if (tmp.AnswerImage?.Length > 0) {
                AnswerTextTMP = tmp.AnswerImage;
            }
            else {
                AnswerTextTMP = tmp.AnswerText[LogLanguage];
            }

            QuestionLog.ParticipantsAnswerReponse response = new QuestionLog.ParticipantsAnswerReponse {
                AnswerId = answerIndex,
                AnswerText = AnswerTextTMP,
                StartTimeQuestion = LastParticipantStartTimes[po],
                StopTimeQuestion = DateTime.Now,
                Attempts = 1
            };
            if (!CurrentScenarioLog.QuestionResults[id].ParticipantsResponse.ContainsKey((char)po)) {
                CurrentScenarioLog.QuestionResults[id].ParticipantsResponse.Add((char)po, response);
            }
            else {
                response.Attempts += CurrentScenarioLog.QuestionResults[id].ParticipantsResponse[(char)po].Attempts;
                CurrentScenarioLog.QuestionResults[id].ParticipantsResponse[(char)po] = response;
                Debug.Log("Overwritting Previously recorded answer");
            }

            if (activeQuestionList[id].Answers.First(s => s.index == answerIndex).FinishQN) {
                {
                    SendEndQuestionnaire(po, lang);
                    return;
                }
            }

            //Debug.Log("There should be some storage code here: " + po + id.ToString() + "  " + answerIndex.ToString());
        }

        SendNewQuestion(po, lang);
        ParticipantCountCurrent[po]++;
        UpdateTotalParticipantCounts();
    }


    public void StopScenario(RerunManager activeManager) {
        Debug.Log("Stopping Scenario and QN logger.");
        CurrentScenarioLog.Stop();
        string folderPath = DataStoragePathSupervisor.GetQNDirectory();
        string fileName = $"Scenario-{CurrentScenarioLog.ScenarioName}_" +
                          $"sessionName-{CurrentScenarioLog.participantComboName}_" +
                          $"{System.DateTime.Now.ToString(DataStoragePathSupervisor.DateTimeFormatFolder)}.json";
        string fullPath = Path.Join(folderPath, fileName);
        string s = JsonConvert.SerializeObject(CurrentScenarioLog, JsonDateSerelizerSettings);
        File.WriteAllText(fullPath, s);
        qnDisplays.Clear();
    }

    private void SendEndQuestionnaire(ParticipantOrder po, string lang) {
        NetworkedQuestionnaireQuestion outval = NetworkedQuestionnaireQuestion.GetDefaultNQQ();
        SendNewQuestionToParticipant(po, outval);
        ParticipantCountCurrent[po] = activeQuestionList.Count(x => x.Value.ContainsOrder(po));
        UpdateTotalParticipantCounts();
    }

    private void SendNewQuestion(ParticipantOrder po, string lang) {
        int val = participantAnswerStatus[po];
        val++;

        while (activeQuestionList.ContainsKey(val) && !activeQuestionList[val].ContainsOrder(po)) {
            Debug.Log($"Looking for a next relevant question! {val}. {activeQuestionList[val].ContainsOrder(po)}");

            val++;
        }

        NetworkedQuestionnaireQuestion outval;
        if (!activeQuestionList.ContainsKey(val)) {
            outval = NetworkedQuestionnaireQuestion.GetDefaultNQQ();
            Debug.Log("Finished questionnaire sending a finish message!! " + val.ToString());
        }
        else {
            participantAnswerStatus[po] = val;
            outval = activeQuestionList[val].GenerateNetworkVersion(lang);
            Debug.Log("Found another question to send: " + val.ToString());
            LastParticipantStartTimes[po] = DateTime.Now;
        }

        SendNewQuestionToParticipant(po, outval);
    }

    private void SendPreviousQuestion(ParticipantOrder p, string lang) {
        int val = participantAnswerStatus[p];
        val--;


        while (activeQuestionList.ContainsKey(val) && !activeQuestionList[val].ContainsOrder(p)) {
            val--;
            Debug.Log("Looking for a next relevant question; " + val.ToString());
        }

        NetworkedQuestionnaireQuestion outval;
        if (!activeQuestionList.ContainsKey(val)) {
            participantAnswerStatus[p] = StartID;
            SendNewQuestion(p, lang);
            Debug.LogWarning(
                "Hit the beginning of the Questionnaire Attempting to send the first message again. " +
                "This is an error should not happen: " +
                val.ToString());
            return;
        }
        else {
            participantAnswerStatus[p] = val;
            outval = activeQuestionList[val].GenerateNetworkVersion(lang);
            Debug.Log("Found Another question to send " + val.ToString());
            LastParticipantStartTimes[p] = DateTime.Now;
        }

        SendNewQuestionToParticipant(p, outval);
    }

    public string GetCurrentQuestionForParticipant(ParticipantOrder po) {
        if (participantAnswerStatus == null || activeQuestionList == null) return " - ";

        if (participantAnswerStatus.ContainsKey(po)
            && activeQuestionList.ContainsKey(participantAnswerStatus[po])
            && activeQuestionList[participantAnswerStatus[po]].QuestionText != null) {
            return activeQuestionList[participantAnswerStatus[po]].QuestionText[LogLanguage] + " at Scenario_ID: " +
                   //  activeQuestionList[participantAnswerStatus[po]].Scenario_ID + "  =>  " +
                   GetTotalParticiapnAnswerCountString(po);
        }


        return "Not Found";
    }

    private Dictionary<ParticipantOrder, string> ParticipantCountStrings = new Dictionary<ParticipantOrder, string>();
    private Dictionary<ParticipantOrder, int> ParticipantCountCurrent = new Dictionary<ParticipantOrder, int>();

    private string GetTotalParticiapnAnswerCountString(ParticipantOrder po) {
        if (ParticipantCountStrings.ContainsKey(po)) {
            return ParticipantCountStrings[po];
        }

        return "";
    }

    private void UpdateTotalParticipantCounts() {
        foreach (ParticipantOrder po in participantAnswerStatus.Keys) {
            if (!ParticipantCountStrings.ContainsKey(po)) {
                ParticipantCountStrings.Add(po, "");
            }

            ParticipantCountStrings[po] =
                ParticipantCountCurrent[po].ToString() +
                " / " + activeQuestionList.Count(x => x.Value.ContainsOrder(po));
        }
    }


    //ImageStorage
    public const string imageDataPrefix = "QNIMAGE";
    public const int ByteArraySize = 1000;

    private Dictionary<ParticipantOrder, List<byte>> ImageRecievers =
        new Dictionary<ParticipantOrder, List<byte>>();

    public void SetupForNewRemoteImage(ParticipantOrder po) {
        if (!ImageRecievers.ContainsKey(po)) {
            ImageRecievers.Add(po, null);
        }

        ImageRecievers[po] = new List<byte>();
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(imageDataPrefix + po.ToString(),
            (senderClientId, reader) => {
                reader.ReadNetworkSerializable<BasicByteArraySender>(out BasicByteArraySender data);

                var participantOrder = ConnectionAndSpawning.Singleton.GetParticipantOrderClientId(senderClientId);
                if (ImageRecievers.ContainsKey(participantOrder)) {
                    foreach (byte b in data.DataSendArray) {
                        ImageRecievers[po].Add(b);
                    }
                }
            });
    }

    public void DeRegisterHandler(ParticipantOrder po) {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(imageDataPrefix + po.ToString());
    }

    public void NewRemoteImage(ParticipantOrder po, int totalSize) {
        if (ImageRecievers.ContainsKey(po)) {
            Debug.Log("Will try To store picture!");


            string folderpath = DataStoragePathSupervisor.GetScreenshotPathDirectory();
            string filename = $"QNImage_Scenario-{CurrentScenarioLog.ScenarioName}_" +
                              $"sessionName-{CurrentScenarioLog.participantComboName}_" +
                              $"ParticipantOrder-{po}_" +
                              $"{DateTime.Now.ToString(DataStoragePathSupervisor.DateTimeFormatFolder)}" +
                              $".jpg";

            string fullPath = Path.Join(folderpath, filename);
            File.WriteAllBytes(fullPath, ImageRecievers[po].ToArray());
            ImageRecievers[po].Clear();
        }
    }


    public void SendNewQuestionToParticipant(ParticipantOrder participantOrder, NetworkedQuestionnaireQuestion outval) {
        qnDisplays[participantOrder].SendNewQuestionClientRPC(outval);
    }


    public void SendTotalQNCount(ParticipantOrder participantOrder, int count) {
        if (participantOrder == ParticipantOrder.None) {
            return;
        }

        Debug.Log($"Total Questions count sent to the participant: {count} for ParticipantOrder{participantOrder}");
        qnDisplays[participantOrder]
            .SetTotalQNCountClientRpc(count);
    }

    public void RegisterQNScreen(ParticipantOrder po, QN_Display qnmanager) {
        Debug.Log($"Got a QN_Display for po{po}");
        qnDisplays.Add(po, qnmanager);
    }
}


public enum LogFlag {
    POSE,
    PARTICIPANTORDER,
    MATERIALCHANGE,
    SPEEDOMETER,
    GPS
}

public struct LogHeader {
    public string name { get; set; }
    public char po { get; set; }

    public int m_ReplayObject { get; set; }
    public LogFlag logFlag { get; set; }
}

public struct QuestionLog {
    public string ID { get; set; }
    public string QuestionText { get; set; }

    public string Research_Info { get; set; } //This property can be used to filter a group of questions

    public Dictionary<char, ParticipantsAnswerReponse> ParticipantsResponse { get; set; }

    public struct ParticipantsAnswerReponse {
        public int AnswerId;
        public string AnswerText;
        public DateTime StartTimeQuestion;
        public DateTime StopTimeQuestion;
        public ushort Attempts;

        public void FinalUpdate(int answerId, string answerText) {
            StopTimeQuestion = DateTime.Now;
            AnswerId = answerId;
            AnswerText = answerText;
        }

        public void SetStartTimeQuestion(DateTime now) {
            StartTimeQuestion = now;
        }
    }

    public QuestionLog(QuestionnaireQuestion qIn) {
        ID = qIn.ID;
        QuestionText = qIn.QuestionText[QNDataStorageServer.LogLanguage];
        Research_Info = qIn.Research_Info;
        ParticipantsResponse = new Dictionary<char, ParticipantsAnswerReponse>();
    }
}

public struct ScenarioLog {
    public DateTime startTime { get; set; } //Timestamp when the scenario started
    public DateTime endTime { get; set; } //Timestamp when the scenario finished and the questionnaire started

    public char
        participantTriggeredQuestionnaire {
        get;
        set;
    } //The participant that walked into the invisible box that initiated the questionnaire. Could be more than 1

    public string ScenarioName { get; set; }

    public string
        participantComboName {
        get;
        set;
    } //I didn't use the term pair to have the flexability for more than 2 participant

    public Dictionary<string, string>
        facts {
        get;
        set;
    } //Dictionary collection of all objective facts we would like to record next to the question for further examination. 

    public Dictionary<int, QuestionLog> QuestionResults { get; set; }

    public ScenarioLog(string name, string participantComboName_) {
        startTime = DateTime.Now;
        endTime = DateTime.MaxValue;
        participantTriggeredQuestionnaire = '-';
        ScenarioName = name;
        participantComboName = participantComboName_;
        facts = new Dictionary<string, string>();
        QuestionResults = new Dictionary<int, QuestionLog>();
    }

    public void Stop() {
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


/*



                if (!ReplayToParticipantAssociation.ContainsKey(id.IDString))
                {
                    IList<ReplayComponentData> data =
                        objectSerializer.ComponentStates.Where(
                            x => ReplaySerializers.GetSerializerTypeFromID(x.ComponentSerializerID) ==
                                 typeof(ParticipantOrderReplayComponent)).ToList();

                    if (data.Any())
                    {
                        GameObject temp = new GameObject("Temp Deserialize");
                        temp.hideFlags = HideFlags.HideAndDontSave;

                        ParticipantOrderReplayComponent comp =
                            temp.AddComponent<ParticipantOrderReplayComponent>();
                        data[0].DeserializeComponent(comp);
                        ReplayToParticipantAssociation.Add(id.IDString, comp.GetParticipantOrder());
                      //  Debug.Log(id.IDString + comp.GetParticipantOrder().ToString());
                        DestroyImmediate(temp);
                    }
                    else
                    {
                        Debug.Log("Did Not Find A po for this object (yet)");
                    }
                }*/

/* if (serializerType == typeof(ReplayTransformSerializer))
           {
               ReplayTransformSerializer transformSerializer = new ReplayTransformSerializer();
               if (!data.DeserializeComponent(transformSerializer))
               {
                   //Debug.Log("transform failiure ");
               }
               else
               {
                   // Debug.Log("transform");
               }

               Debug.Log("Pos: " + transformSerializer.Position);
               //  Debug.Log("Rot: " + transformSerializer.Rotation);
               //  Debug.Log("Scale: " + transformSerializer.Scale);
           }
           else if (serializerType == typeof(ReplayMaterialChange))
           {
               GameObject temp = new GameObject("TempDeserialize");
               temp.hideFlags = HideFlags.HideAndDontSave;

               ReplayMaterialChange comp = temp.AddComponent<ReplayMaterialChange>();

               Debug.Log(data.DeserializeComponent(comp));

               Debug.Log("Deserialized Material: " + comp.GetAssignedMaterialIndex());

               DestroyImmediate(temp);
           }
           else if (serializerType == typeof(ParticipantOrderReplayComponent))
           {
               ParticipantOrderReplayComponent mchangeSer =
                   new ParticipantOrderReplayComponent();

               if (!data.DeserializeComponent(mchangeSer))
               {
                   Debug.Log("ParticipantOrder failiure");
               }
               else
               {
                   Debug.Log("ParticipantOrder success");
               }
           }*/


/*
Debug.Log(target.InitialStateBuffer.Identities.Count());

Debug.Log("ParticipantOrderReplayComponent: " +
      ReplaySerializers.GetSerializerIDFromType(typeof(ParticipantOrderReplayComponent)));
Debug.Log("generatedSerilizer Class: " + ReplaySerializers.GetSerializerTypeFromID(
ReplaySerializers.GetSerializerIDFromType(typeof(ParticipantOrderReplayComponent))));


Debug.Log("ReplayMaterialChange: " + ReplaySerializers.GetSerializerIDFromType(typeof(ReplayMaterialChange)));
Debug.Log("ReplayMaterialChangeSerializer: " +
      ReplaySerializers.GetSerializerIDFromType(typeof(ReplayMaterialChangeSerializer)));

Debug.Log("ReplayTransform: " + ReplaySerializers.GetSerializerIDFromType(typeof(ReplayTransform)));
Debug.Log("ReplayTransformSerializer: " +
      ReplaySerializers.GetSerializerIDFromType(typeof(ReplayTransformSerializer)));

Dictionary<float, Dictionary<string, string>> MainDataLog = new Dictionary<float, Dictionary<string, string>>();
if (target.CanRead)
{
float count = 0f;
ReplayFileJsonTarget target2;
while (count <= target.Duration)
{
    ReplaySnapshot snapshot;
    snapshot = target.FetchSnapshot(count);
    if (!MainDataLog.ContainsKey(snapshot.TimeStamp))
    {
        MainDataLog.Add(snapshot.TimeStamp, new Dictionary<string, string>());
    }

    foreach (ReplayIdentity id in snapshot.Identities)
    {
        ReplayState state = snapshot.RestoreSnapshot(id);
        ReplayObjectSerializer objectSerializer = new ReplayObjectSerializer();
        objectSerializer.OnReplayDeserialize(state);

        foreach (var tmp in objectSerializer.EventStates)
        {
//                        Debug.Log(id.ToString()+tmp.BehaviourIdentity.ToString() + tmp.EventID.ToString());
        }

        foreach (var tmp in objectSerializer.VariableStates)
        {
            //  Debug.Log(id.ToString()+tmp.BehaviourIdentity.ToString());
        }


        foreach (ReplayComponentData data in objectSerializer.ComponentStates)
        {
            Debug.Log(data.BehaviourIdentity.IDString);

            Type serializerType = data.ResolveSerializerType();
            ReplaySerializers.GetSerializerTypeFromID(data.ComponentSerializerID);
            Debug.Log(serializerType + "<< serializerType  and int IDs>>" + data.ComponentSerializerID);


            if (MetaData.ComponentHeader.ContainsKey(data.BehaviourIdentity.IDValue))
            {
                LogHeader tmp = MetaData.ComponentHeader[data.BehaviourIdentity.IDValue];
                switch (tmp.logFlag)
                {
                    case LogFlag.POSE:
                        ReplayTransformSerializer transformSerializer = new ReplayTransformSerializer();
                        if (!MainDataLog[snapshot.TimeStamp].ContainsKey(GenerateHeader(tmp)))
                        {
                            MainDataLog[snapshot.TimeStamp].Add(GenerateHeader(tmp),
                                transformSerializer.Position.ToString() + "_" +
                                transformSerializer.Rotation.eulerAngles.ToString());
                        }
                        else
                        {
                            MainDataLog[snapshot.TimeStamp][GenerateHeader(tmp)]
                                += "__"
                                   + transformSerializer.Position.ToString() + "_"
                                   + transformSerializer.Rotation.eulerAngles.ToString();
                        }

                        break;
                    case LogFlag.PARTICIPANTORDER:
                        break;
                    case LogFlag.MATERIALCHANGE:
                        break;
                    case LogFlag.SPEEDOMETER:
                        break;
                    case LogFlag.GPS:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }


    count += (1f / 15f);
    yield return null;
}
}
*/