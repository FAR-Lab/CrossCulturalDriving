using System.Collections;
using UnityEngine;
using System.IO;
using System.Collections.Generic;


public class LoadCenterline : MonoBehaviour
{

    public ScenarioSelector scenario;

    public List<CLPoints> centerline = new List<CLPoints>();

    public bool LoadNewCLButton = false;

    [SerializeField]
    private SplineCenterlineUtility SplineCreator;

    [SerializeField]
    private List<string> CenterlineFiles = new List<string>
    {
        "centerline_CP1_A.csv", "centerline_CP1_B.csv", "centerline_CP2_A.csv", "centerline_CP2_B.csv",
        "centerline_CP3_A.csv", "centerline_CP3_B.csv", "centerline_CP5_A.csv", "centerline_CP5_B.csv",
        "centerline_CP6_A.csv", "centerline_CP6_B.csv", "centerline_CP7_A.csv", "centerline_CP7_B.csv",
        "centerline_CP8_A.csv", "centerline_CP8_B.csv"
    };

    private void Start()
    {
        centerline = ReadCenterLineCSV();
        SplineCreator.LoadPointsFromCL(centerline);
    }

    [ContextMenu("Load Path")]
    public void LoadPath()
    {
        centerline = ReadCenterLineCSV();
        SplineCreator.LoadPointsFromCL(centerline);
    }
    
    private void Update()
    {
        if (LoadNewCLButton)
        {
            LoadNewCLButton = false;
            centerline = ReadCenterLineCSV();
            SplineCreator.LoadPointsFromCL(centerline);
        }
    }

    public List<CLPoints> ReadCenterLineCSV()
    {
        string filePath = Application.dataPath + "/Data/Centerlines/" + CenterlineFiles[(int)scenario];

        List<CLPoints> CL = new List<CLPoints>();

        if (File.Exists(filePath))
        {
            StreamReader reader = new StreamReader(filePath);
            string line;

            reader.ReadLine();  // The first line is not input so we read it here

            while ((line = reader.ReadLine()) != null)
            {
                string[] fields = line.Split(',');

                CLPoints tmpCL = new CLPoints(fields[1], fields[2], fields[3]);
                CL.Add(tmpCL);

                //Debug.Log("Added new point for CL: "  + tmpCL.point_id + " , " + tmpCL.HeadPosX + " , " + tmpCL.HeadPosZ);
            }

            reader.Close();
        }
        else
        {
            Debug.LogError("CSV file not found: " + filePath);
        }

        return CL;
    }


    public enum ScenarioSelector
    {
        centerline_CP1_A = 0,
        centerline_CP1_B = 1,
        centerline_CP2_A = 2,
        centerline_CP2_B = 3,
        centerline_CP3_A = 4,
        centerline_CP3_B = 5,
        centerline_CP5_A = 6,
        centerline_CP5_B = 7,
        centerline_CP6_A = 8,
        centerline_CP6_B = 9,
        centerline_CP7_A = 10,
        centerline_CP7_B = 11,
        centerline_CP8_A = 12,
        centerline_CP8_B = 13
    }

}
public class CLPoints
{
    public int point_id;
    public float HeadPosX;
    public float HeadPosZ;
    public CLPoints(string id, string headPosX, string headPosZA)
    {
        point_id = int.Parse(id);
        HeadPosX = float.Parse(headPosX);
        HeadPosZ = float.Parse(headPosZA);
    }
}
