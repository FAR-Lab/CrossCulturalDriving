using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ConfigFileLoading {
    public static string ending = ".conf";

    private string m_path;

    private static char m_seperator = '\t';
    // Start is called before the first frame update

    private bool ready;
    public bool ReadyToLoad { get { return ready; } }

    public void Init(string name) {
        m_path = Application.persistentDataPath + "\\" + name + ending;
        ready = true;
    }

    public bool FileAvalible() { return File.Exists(m_path); }

    public void LoadLocalOffset(out Vector3 pos, out Quaternion rot) {
        bool error = false;
        pos = Vector3.zero;
        rot = Quaternion.identity;
        Dictionary<string, string> dict = LoadDict();
        if (!float.TryParse(dict["posx"], out pos.x)) error = true;
        if (!float.TryParse(dict["posy"], out pos.y)) error = true;
        if (!float.TryParse(dict["posz"], out pos.z)) error = true;
        //
        if (!float.TryParse(dict["rotx"], out rot.x)) error = true;
        if (!float.TryParse(dict["roty"], out rot.y)) error = true;
        if (!float.TryParse(dict["rotz"], out rot.z)) error = true;
        if (!float.TryParse(dict["rotw"], out rot.w)) error = true;
        if (error) { Debug.Log("Loading the offset didn't really work :-/"); }
    }

    public void StoreLocalOffset(Vector3 pos, Quaternion rot) {
        Dictionary<string, float> dict = new Dictionary<string, float>();
        dict["posx"] = pos.x;
        dict["posy"] = pos.y;
        dict["posz"] = pos.z;
        //
        dict["rotx"] = rot.x;
        dict["roty"] = rot.y;
        dict["rotz"] = rot.z;
        dict["rotw"] = rot.w;
        //
        WriteDict(dict);
    }

    // https://stackoverflow.com/questions/59288414/how-do-i-use-streamreader-to-add-split-lines-to-a-dictionary?rq=1

    private Dictionary<string, string> LoadDict() {
        Debug.Log("Trying to load from: " + m_path);
        Dictionary<string, string> dict = File
            .ReadLines(@m_path)
            .Where(line => !string.IsNullOrEmpty(line)) // to be on the safe side
            .Select(line => line.Split(m_seperator))
            .ToDictionary(items => items[0], items => items[1]);

        return dict;
    }

    private void WriteDict(Dictionary<string, float> dict) {
        Debug.Log("Trying to write to: " + m_path);
        StreamWriter writer = new StreamWriter(m_path, false);
        foreach (KeyValuePair<string, float> entry in dict) {
            writer.WriteLine(entry.Key + m_seperator + entry.Value.ToString());
        }

        writer.Close();
    }

    public bool DeleteFile() {
        if (ready) {
            File.Delete(m_path);
            return true;
        }
        else { return false; }
    }
}