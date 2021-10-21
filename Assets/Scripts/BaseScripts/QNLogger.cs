using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using UnityEngine;


public class QNLogger : MonoBehaviour
{
   
    
    public static char sep = ';'; //Separator for data values.
    private string path; //Location of the log files
    private Thread send; //Independent thread for writing and sending data from databuffer
    private StreamWriter sw;

    private static QNLogger _instance;
    public static QNLogger Instance { get { return _instance; } }
   
    void Start()
    {
        
        path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string identifier = String.Format("{0:yyyymmdd-HHmmss}.txt", System.DateTime.Now);
        sw  = new StreamWriter(Path.Combine( path, identifier));
        // sw = File.AppendText(newPath + participantID + ".csv");
    }

    public void AddNewDataPoint(QuestionnaireQuestion qq,int AnswerIndex,LanguageSelect lang) {

        string temp = String.Format(
            "Time: {0:g}, Questions: {1}, Answer: {2}, Language {3}.",
            System.DateTime.Now, qq.QuestionText[lang], qq.Answers[AnswerIndex].AnswerText[lang], lang.ToString());
        //qq.SA_Level  
        //qq.Behavior
        sw.WriteLine(temp);
    }
    void OnDestroy() {
        sw.Flush();
        sw.Close();
    }



}
