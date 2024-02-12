using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;


public static class DataStoragePathSupervisor {
    private static ServerApplicationConfiguration _config;
    private static string StudyName = "";
    private static bool nameSet = false;

    
    public static string DateTimeFormatFolder = "yyyy-MM-dd-HH-mm-ss";
    public static string DateTimeFormatInternal = "yyyy-MM-dd-HH:mm:ss.ffffffzzz";
    
    
    
    
    static DataStoragePathSupervisor() {
        // Construct the path to the configuration file within the Assets folder
        string configFilePath = Path.Combine(Application.dataPath, "ServerApplicationConfig.json");

        // Load paths from the JSON file
        LoadPathsFromJson(configFilePath);
    }

    private static void LoadPathsFromJson(string filePath) {
        if (!File.Exists(filePath)) {
            var t = new ServerApplicationConfiguration { //MicroTODO: move this to the struct constructor to create default.
                BasePath = Application.dataPath, CsvSubPath = "csv",QNSubPath = "QN",RerunSubPath = "ReRun", RerunMetaSubPath = "ReRunMeta",
                ScreenshotPath = "ScreenShots", VideoRecordingPath = "VideoRecordings", MiscDataPath = "misc"
            };
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath)) {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(filePath, JsonConvert.SerializeObject(t));
        }

        try {
            string jsonText = File.ReadAllText(filePath);
            _config = JsonConvert.DeserializeObject<ServerApplicationConfiguration>(jsonText);
        }
        catch (Exception ex) {
            Debug.LogError($"Error loading configuration: {ex.Message}");
          
        }
    }

    
    public static string GetJSONPath() {
       return Path.Combine(Application.dataPath, "ServerApplicationConfig.json");

    }
    public static void setStudyName(string s) {
        StudyName = s;
        nameSet = true;
    }

    private static string EnsureDir(string path) {
        if (!Directory.Exists(path)) {
            Directory.CreateDirectory(path);
        }

        return path;
    }

    private static string i_sn() {
        if (nameSet) {
            return StudyName;
        }
        else {
              setStudyName($"UnNamed at{DateTime.Now.ToString(DateTimeFormatFolder)}");
        }

        return StudyName;
    }
    public static string GetCsvDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.CsvSubPath);
        return EnsureDir(tmp);
    }
    public static string GetQNDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.QNSubPath);
        return EnsureDir(tmp);
    }
    public static string GetReRunDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.RerunSubPath);
        return EnsureDir(tmp);
    }
    public static string GetReRunMetaDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.RerunMetaSubPath);
        return EnsureDir(tmp);
    }
    public static string GetScreenshotPathDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.ScreenshotPath);
        return EnsureDir(tmp);
    }
    public static string GetVideoRecordingPathDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.VideoRecordingPath);
        return EnsureDir(tmp);
    }
    public static string GetMiscDataPathDirectory() {
        string tmp = Path.Join(_config.BasePath, i_sn(), _config.MiscDataPath);
        return EnsureDir(tmp);
    }
    
    
}