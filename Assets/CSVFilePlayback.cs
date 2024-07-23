using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class CSVFilePlayback : MonoBehaviour {
    public TextAsset File;

    public VehicleController aiCar;
    // Start is called before the first frame update

    private Vector3 startPosition;
    
    private int rowCounter;
    private int maxRow;
    private float nextTime;
    
    private float[] AccelColumn,SteeringColumn,ScenarioTimeColumn;
    IEnumerator Start() {
        Debug.Log(File.bytes);
        yield return new WaitUntil(() =>
            File != null
        );
        ReadingInFile(File);
    }

    private void ReadingInFile(TextAsset theFile) {
        List<string> lines = new List<string>(Regex.Split(theFile.text, "\n|\r|\r\n"));
        AccelColumn = new float[lines.Count-1]; // -1 for the first line of headers
        SteeringColumn = new float[lines.Count-1];
        ScenarioTimeColumn = new float[lines.Count-1];
       
        var headers = lines[0].Split(";");
        var accelColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("AccelB")).Index;
        var steeringColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("SteerB")).Index;
        var scenarioTimeColumnId = headers.Select((item, i) => new { Item = item, Index = i })
            .First(x => x.Item.Contains("ScenarioTime")).Index;
     
        for( int c=1; c<lines.Count;c++){
            var elements = lines[c].Split(";");
            if (c == 1) {
                bool suc = true;
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosXA")), out float ai_x);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosYA")), out float ai_y);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosZA")), out float ai_z);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadrotYA")), out float ai_y_rot);
                    
                
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosXB")), out float x);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosYB")), out float y);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadPosZB")), out float z);
                suc &= float.TryParse(elements.First(x => x.Contains("HeadrotYB")), out float y_rot);

                if (suc) {
                    Debug.Log("found all starting locations");
                }
                else {
                    string s="";
                    Array.ForEach(elements, x => s += x.ToString());
                    Debug.LogError ("Didnt find everything here is the buffer: "+ s);
                }
            }
            AccelColumn[c-1] = float.Parse(elements[accelColumnId]);
            SteeringColumn[c-1] = float.Parse(elements[steeringColumnId]);
            ScenarioTimeColumn[c-1] = float.Parse(elements[scenarioTimeColumnId]);
        }
    }
}
