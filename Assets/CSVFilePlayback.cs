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
    
    private float[] AccelColume,SteeringColume,ScenarioTimeColume;
    void Start() {
        Debug.Log(File.bytes);
    }

    private IEnumerator ReadingInFile(TextAsset theFile) {
        return null;
        /*
        List<string> lines = new List<string>(Regex.Split(theFile.text, "\n|\r|\r\n"));
        AccelColume = new float[lines.Count-1]; // -1 for the first line of headers
        SteeringColume = new float[lines.Count-1];
        ScenarioTimeColume = new float[lines.Count-1];
        int AccelColumeid,SteeringColumeid,ScenarioTimeColumeid;
       
     
        for( int c=0; c<lines.Count;c++){
            var elements = lines[c].Split(";");
            if (c == 0) {
                AccelColumeid = elements.Select((item, i) => new { Item = item, Index = i })
                    .First(x => x.Item.Contains("AccelB")).Index;
                SteeringColumeid = elements.Select((item, i) => new { Item = item, Index = i })
                    .First(x => x.Item.Contains("SteerB")).Index;
                ScenarioTimeColumeid = elements.Select((item, i) => new { Item = item, Index = i })
                    .First(x => x.Item.Contains("ScenarioTime")).Index;
            }
            else{
                if (c == 1) {
                    
                    bool suc = true;
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosXA"), out float ai_x));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosYA"), out float ai_y));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosZA"), out float ai_z));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadrotYA"), out float ai_y_rot));
                        
                    
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosXB"), out float x));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosYB"), out float y));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadPosZB"), out float z));
                    suc &= float.TryParse(elements.First(x => x.Contains("HeadrotYB"), out float y_rot));

                    if (suc) {
                        Debug.Log("found all starting locations");
                    }
                    else {
                        string s="";
                        elements.ForEach(x => s + x.tostrtring());
                        Debug.LogError ("Didnt find everything here is the buffer: "+ s);
                    }
                    
                    
                    AccelColume[c-1] = elements[AccelColumeid];
                   SteeringColume = elements[SteeringColumeid];
                    ScenarioTimeColume = elements[ScenarioTimeColumeid];
            }

           
        }
            
        }
*/
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
