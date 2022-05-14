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
                StartCoroutine(CaptureScreenShot());
            }
        }
    }
    IEnumerator CaptureScreenShot()
    {
        // We should only read the screen buffer after rendering is complete
        yield return new WaitForEndOfFrame();

        // Create a texture the size of the screen, RGB24 format

        CapturedScenarioImage = ScreenCapture.CaptureScreenshotAsTexture();
    }


    public Texture2D GetTexture(){
      return  CapturedScenarioImage;
    }
}
