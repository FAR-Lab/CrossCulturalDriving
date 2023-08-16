using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Serialization;

public class VRUIManager : MonoBehaviour
{
    public TMP_Dropdown ParticipantDropdown;
    public TMP_Text ParticipantText;
    public TMP_Dropdown LanguageDropdown;
    public TMP_Text LanguageText;
    public TMP_Text ServerIPText;
    public TMP_InputField ServerIPField;
    public Button saveConfigButton;

    public string defaultServerIP = "192.168.";
    public string fileName = "ParticipantConfig.json";

    private ParticipantConfig currentConfig;

    [Serializable]
    public struct ParticipantConfig
    {
        public string ServerIPString;
        public string ParticipantIDString;
        public string LanguageString;
    }

    private void Start()
    {
        PopulateUI(); 
        TryLoadConfiguration();
        SetCurrentPlatform();
    }

    private void Update()
    {
        // debug log the current config if it exists
        if (currentConfig.ParticipantIDString != null)
        {
            Debug.Log("Current Config: " + currentConfig.ParticipantIDString + " " + currentConfig.LanguageString + " " + currentConfig.ServerIPString);
        }
    }

    void PopulateUI()
    {
        saveConfigButton.onClick.AddListener(delegate { SaveConfiguration(); });

        PopulateDropdown(ParticipantDropdown, new List<string> { "A", "B", "C", "D", "E", "F" });
        PopulateDropdown(LanguageDropdown, new List<string> { "English", "Hebrew", "Chinese", "German" });
  
        // set text field to default value
        ServerIPField.text = defaultServerIP;
        ServerIPText.text = "IP: " + defaultServerIP;
        
        // set dropdowns to default values
        ParticipantDropdown.value = 0;
        LanguageDropdown.value = 0;

        ParticipantDropdown.onValueChanged.AddListener(delegate { UpdateDropdownValue(ParticipantDropdown, ParticipantText, "ID: ", ref currentConfig.ParticipantIDString); });
        LanguageDropdown.onValueChanged.AddListener(delegate { UpdateDropdownValue(LanguageDropdown, LanguageText, "Lang: ", ref currentConfig.LanguageString); });
        ServerIPField.onValueChanged.AddListener(delegate { UpdateInputFieldValue(ServerIPField, ServerIPText, "IP: ", ref currentConfig.ServerIPString); });
    }

    void PopulateDropdown(TMP_Dropdown dropdown, List<string> items)
    {
        dropdown.options.Clear();
        foreach (var item in items)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData() { text = item });
        }
    }

    void UpdateDropdownValue(TMP_Dropdown dropdown, TMP_Text textBox, string prefix, ref string targetField)
    {
        int index = dropdown.value;
        if(index >= 0 && index < dropdown.options.Count)
        {
            string value = dropdown.options[index].text;
            textBox.text = prefix + value;
            targetField = value;
        }
    }

    void UpdateInputFieldValue(TMP_InputField inputField, TMP_Text textBox, string prefix, ref string targetField)
    {
        string value = inputField.text;
        textBox.text = prefix + value;
        targetField = value;
    }

    private void SaveConfiguration()
    {
        // prevent null values from being saved
        if (currentConfig.ServerIPString == null)
        {
            currentConfig.ServerIPString = "192.168.";
        }
        
        string jsonString = JsonConvert.SerializeObject(currentConfig);
        File.WriteAllText(GetFilePath(), jsonString);
        
        Application.Quit();
    }

    private void TryLoadConfiguration()
    {
        if (File.Exists(GetFilePath()))
        {
            LoadConfiguration();
        }
        else
        {
            ParticipantDropdown.value = 0;
            LanguageDropdown.value = 0;
            ServerIPField.text = "";
        }
    }

    private void LoadConfiguration()
    {
        string jsonString = File.ReadAllText(GetFilePath());
        currentConfig = JsonConvert.DeserializeObject<ParticipantConfig>(jsonString);
        
        // if any of the values are null, set them to default values
        if (currentConfig.ServerIPString == null)
        {
            currentConfig.ServerIPString = "192.168.";
        }

        // Update UI
        ServerIPField.text = currentConfig.ServerIPString;

        ParticipantDropdown.SetValueWithoutNotify(ParticipantDropdown.options.FindIndex(option => option.text == currentConfig.ParticipantIDString));
        LanguageDropdown.SetValueWithoutNotify(LanguageDropdown.options.FindIndex(option => option.text == currentConfig.LanguageString));

        UpdateDropdownValue(ParticipantDropdown, ParticipantText, "ID: ", ref currentConfig.ParticipantIDString);
        UpdateDropdownValue(LanguageDropdown, LanguageText, "Lang: ", ref currentConfig.LanguageString);
        UpdateInputFieldValue(ServerIPField, ServerIPText, "IP: ", ref currentConfig.ServerIPString);
    }

    string GetFilePath()
    {
        return Application.persistentDataPath + "/" + fileName;
    }

    void SetCurrentPlatform()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
            case RuntimePlatform.WindowsPlayer:
                // Do something specific for Windows
                break;
            case RuntimePlatform.Android:
                // Do something specific for Android
                break;
            case RuntimePlatform.LinuxPlayer:
                // Do something specific for Linux
                break;
            default:
                Debug.LogError("Platform not supported!");
                break;
        }
    }
}
