using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class ServerChange : MonoBehaviour {
    public TMP_Dropdown ParticipantDropdown;
    public TMP_Text ParticipantTextBox;
    public TMP_Dropdown LanguageDropdown;
    public TMP_Text LanguageTextBox;
    public TMP_Text ServerIPTextBox;
    public TMP_Text ServerConnectionStatusTextBox;
    public TMP_InputField serverIPField;

    public GameObject OVRCamera;
    public GameObject NetworkManager;

    public string FileName = "Participant.conf";
    private string LanguageString;

    private string m_path;
    private readonly char m_seperator = '\t';
    private string ParticipantIDString;

    private string ServerIPString;


    public bool ReadyToLoad { get; private set; }

    // This script renders UI compnjents with textmesh pro for changing participant setup configuration on the oculus headset
    // Setup the scene as described in https://www.youtube.com/watch?v=1zJw0F_3UZ0

    private void Start() {
        m_path = Application.persistentDataPath + "/" + FileName;
        ReadyToLoad = true;

        ParticipantDropdown.options.Clear();

        var ParticipantItems = new List<string>();
        ParticipantItems.Add("A");
        ParticipantItems.Add("B");
        ParticipantItems.Add("C");
        ParticipantItems.Add("D");
        ParticipantItems.Add("E");
        ParticipantItems.Add("F");

        foreach (var item in ParticipantItems)
            ParticipantDropdown.options.Add(new TMP_Dropdown.OptionData { text = item });

        LanguageDropdown.options.Clear();

        var LanguageItems = new List<string>();
        LanguageItems.Add("English");
        LanguageItems.Add("Hebrew");
        LanguageItems.Add("Chinese");
        LanguageItems.Add("German");

        foreach (var item in LanguageItems) LanguageDropdown.options.Add(new TMP_Dropdown.OptionData { text = item });

        if (LoadConf()) AutoStartStudy();

        ParticipantDropdown.onValueChanged.AddListener(
            delegate { ParticipantDropdownItemSelected(ParticipantDropdown); });


        LanguageDropdown.onValueChanged.AddListener(delegate { LanguageDropdownItemSelected(ParticipantDropdown); });

        serverIPField.onValueChanged.AddListener(delegate { IPCHanged(); });
    }


    public void IPCHanged() {
        ServerIPString = serverIPField.text;
        ServerIPTextBox.text = "IP: " + ServerIPString;
    }

    public void StoreConfiguration() {
        StoreConf(ParticipantIDString, ServerIPString, LanguageString);
        Debug.Log("Store configuration file and shut down.");
        Application.Quit();
    }

    private void AutoStartStudy() {
        ServerConnectionStatusTextBox.text = "Trying to establish a connection";
        if (Enum.TryParse(ParticipantIDString, out ParticipantOrder myParticipant)) {
            ConnectionAndSpawning.Singleton.StartAsClient(LanguageString, myParticipant, ServerIPString, 7777,
                ResponseDelegate);
            ServerConnectionStatusTextBox.text = "Waiting for Connection Response";
        }

        else {
            Debug.LogError("ServerChange could not find a matching particpant ID");
            ServerConnectionStatusTextBox.text = "No Matching Participant ID Enum";
        }
    }

    private void ResponseDelegate(ConnectionAndSpawning.ClientConnectionResponse response) {
        switch (response) {
            case ConnectionAndSpawning.ClientConnectionResponse.FAILED:
                Debug.Log("Connection Failed maybe change IP address, participant order (A,b,C, etc.) or the port");
                ServerConnectionStatusTextBox.text = "Connection failed";
                break;
            case ConnectionAndSpawning.ClientConnectionResponse.SUCCESS:
                Debug.Log("We are connected you can stop showing the UI now!");
                ServerConnectionStatusTextBox.text = "CONNECTED!";

                Destroy(transform.parent.gameObject);
                break;
        }
    }

    private void ParticipantDropdownItemSelected(TMP_Dropdown dropdown) {
        var index = dropdown.value;
        ParticipantIDString = dropdown.options[index].text;
        ParticipantTextBox.text = "ID: " + ParticipantIDString;
    }

    private void LanguageDropdownItemSelected(TMP_Dropdown dropdown) {
        var index = LanguageDropdown.value;
        LanguageString = LanguageDropdown.options[index].text;
        LanguageTextBox.text = "Lang: " + LanguageString;
    }
    // All for writing and storing data from down here


    private bool LoadConf() {
        if (File.Exists(m_path)) {
            var dict = LoadDict();

            if (dict.ContainsKey("participant") && dict.ContainsKey("server") && dict.ContainsKey("language")) {
                ParticipantIDString = dict["participant"];
                ServerIPString = dict["server"];
                LanguageString = dict["language"];

                switch (ParticipantIDString) {
                    case "A":
                        ParticipantDropdown.value = 0;
                        break;

                    case "B":
                        ParticipantDropdown.value = 1;
                        break;

                    case "C":
                        ParticipantDropdown.value = 2;
                        break;

                    case "D":
                        ParticipantDropdown.value = 3;
                        break;

                    case "E":
                        ParticipantDropdown.value = 4;
                        break;

                    case "F":
                        ParticipantDropdown.value = 5;
                        break;

                    default:
                        Debug.LogError("ServerChange could not find a matching particpant ID");
                        ParticipantDropdown.value = 0;
                        break;
                }

                switch (LanguageString) {
                    case "English":
                        LanguageDropdown.value =
                            0; //TODO This is bad form. We should not directly reference the index but rather dynamically find the index. 
                        break;

                    case "Hebrew":
                        LanguageDropdown.value = 1;
                        break;

                    case "Chinese":
                        LanguageDropdown.value = 2;
                        break;

                    case "German":
                        LanguageDropdown.value = 3;
                        break;

                    default:
                        Debug.LogError("ServerChange could not find a matching language ");
                        LanguageDropdown.value = 0;
                        break;
                }


                return true;
            }

            Debug.LogError("Participant configuration file corrupted");
            return false;
        }

        ParticipantIDString = "A";
        ServerIPString = "192.168.1.1";
        LanguageString = "English";

        ParticipantDropdown.value = 0;
        LanguageDropdown.value = 0;
        return false;
    }

    public void StoreConf(string participantID, string serverIP, string language) {
        var dict = new Dictionary<string, string>();
        dict["participant"] = participantID;
        dict["server"] = serverIP;
        dict["language"] = language;


        WriteDict(dict);
    }

    // https://stackoverflow.com/questions/59288414/how-do-i-use-streamreader-to-add-split-lines-to-a-dictionary?rq=1

    private Dictionary<string, string> LoadDict() {
        Debug.Log("Trying to load from: " + m_path);
        var dict = File
            .ReadLines(m_path)
            .Where(line => !string.IsNullOrEmpty(line)) // to be on the safe side
            .Select(line => line.Split(m_seperator))
            .ToDictionary(items => items[0], items => items[1]);

        return dict;
    }

    private void WriteDict(Dictionary<string, string> dict) {
        Debug.Log("Trying to write to: " + m_path);
        var writer = new StreamWriter(m_path, false);
        foreach (var entry in dict) writer.WriteLine(entry.Key + m_seperator + entry.Value);

        writer.Close();
    }
}