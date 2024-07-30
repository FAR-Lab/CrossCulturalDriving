using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSVReplayDriver : MonoBehaviour {
    public TextAsset File;
    
    private float[] AccelColumn,SteeringColumn,ScenarioTimeColumn;
    private bool[] LeftIndicatorColumn,RightIndicatorColumn,HornColumn;

    private float startTime;
    private float currentTime => Time.time - startTime;
    private int currentRow; //the row where time is just less than the current time
    
    IEnumerator Start() {
        yield return new WaitUntil(() =>
            File != null
        );
        Debug.Log(File.bytes);
        ReadingInFile(File);
        yield return new WaitUntil(() => ConnectionAndSpawning.Singleton.ServerState == ActionState.DRIVE);
        startTime = Time.time;
    }

    private void ReadingInFile(TextAsset theFile) {
        List<string> lines = new List<string>(Regex.Split(theFile.text, "\n|\r|\r\n"));
        int numRows = (lines.Count - 1) / 2;  // -1 for the first line of headers, /2 because of the empty lines
        AccelColumn = new float[numRows];
        SteeringColumn = new float[numRows];
        ScenarioTimeColumn = new float[numRows];
        LeftIndicatorColumn = new bool[numRows];
        RightIndicatorColumn = new bool[numRows];
        HornColumn = new bool[numRows];
       
        var headers = lines[0].Split(";");
        var accelColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("AccelB")).Index;
        var steeringColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("SteerB")).Index;
        var scenarioTimeColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("ScenarioTime")).Index;
        var hornColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("ButtonB")).Index;
        var indicatorColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("IndicatorsB")).Index;
     
        for (int c = 1; c < numRows; c++){
            var elements = lines[c*2].Split(";"); // *2 because of the empty lines
            AccelColumn[c-1] = float.Parse(elements[accelColumnId]);
            SteeringColumn[c-1] = float.Parse(elements[steeringColumnId]);
            ScenarioTimeColumn[c-1] = float.Parse(elements[scenarioTimeColumnId]);
            
            var indicators = elements[indicatorColumnId].Split("_");
            if (indicators.Length == 2) {
                LeftIndicatorColumn[c-1] = indicators[0] == "LeftTrue";
                RightIndicatorColumn[c-1] = indicators[1] == "RightTrue";
            }
            
            HornColumn[c-1] = elements[hornColumnId] == "TRUE";
        }
    }

    public void UpdateRow() {
        while (currentRow + 2 < ScenarioTimeColumn.Length && ScenarioTimeColumn[currentRow + 1] < currentTime) {
            currentRow++;
        } // check for +2 because we need to have the next row available
    }
    
    public float GetSteerInput() {
        return Mathf.Lerp(SteeringColumn[currentRow], SteeringColumn[currentRow + 1],
            (currentTime - ScenarioTimeColumn[currentRow]) / (ScenarioTimeColumn[currentRow + 1] - ScenarioTimeColumn[currentRow]));
    }
    
    public float GetAccelInput() {
        return Mathf.Lerp(AccelColumn[currentRow], AccelColumn[currentRow + 1],
            (currentTime - ScenarioTimeColumn[currentRow]) / (ScenarioTimeColumn[currentRow + 1] - ScenarioTimeColumn[currentRow]));
    }
    
    public bool GetLeftIndicatorInput() {
        return LeftIndicatorColumn[currentRow];
    }
    
    public bool GetRightIndicatorInput() {
        return RightIndicatorColumn[currentRow];
    }
    
    public bool GetHornInput() {
        return HornColumn[currentRow];
    }
}
