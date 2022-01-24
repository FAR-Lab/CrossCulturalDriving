using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System;
using UnityEngine;


public class QNLogger {
    public static string qnMessageName = "QNDATA";
    public static char sep = ';'; //Separator for data values.

    private string output = "";
   
    public void Init() {

        output += String.Format("{0:yyyymmdd-HHmmss}-{1}.txt", System.DateTime.Now,
            ConnectionAndSpawing.Singleton.ParticipantOrder.ToString());

        output += sep;

    }

    public void AddNewDataPoint(QuestionnaireQuestion qq,int AnswerIndex,LanguageSelect lang) {

        string temp = String.Format(
            "Time: {0:g}, Questions: {1}, Answer: {2}, Language {3}.",
            System.DateTime.Now, qq.QuestionText[lang], qq.Answers[AnswerIndex].AnswerText[lang], lang.ToString());
        output +=temp;
        output += sep;
        
        //qq.SA_Level  
        //qq.Behavior

    }
    public void DumpData(out string data) {
        data = output;
    }

   
}
