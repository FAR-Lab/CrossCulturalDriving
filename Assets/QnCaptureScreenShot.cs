using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QnCaptureScreenShot : MonoBehaviour
{

    public List<ParticipantOrder> ParticipantsWeShouldTakeAPictureFor = new List<ParticipantOrder>();
    
    bool triggered = false;

    public Texture2D CapturedScenarioImage;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool ContainsPO(ParticipantOrder po){
        return ParticipantsWeShouldTakeAPictureFor.Contains(po);
    }
    private void OnTriggerEnter(Collider other) {
        if (!triggered) {
            if (ParticipantsWeShouldTakeAPictureFor.Contains(ConnectionAndSpawing.Singleton.ParticipantOrder)){
                triggered = true;
               // CapturedScenarioImage= Application.persistentDataPath + "temp.png";
                CapturedScenarioImage = ScreenCapture.CaptureScreenshotAsTexture(ScreenCapture.StereoScreenCaptureMode.RightEye);
            }

        }
    }


    public Texture2D GetTexture(){
      return  CapturedScenarioImage;
    }
}
