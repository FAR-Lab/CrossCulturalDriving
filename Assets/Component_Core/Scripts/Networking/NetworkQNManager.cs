using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkQNManager : MonoBehaviour
{
    
    [Serializable]
    public class QualtricsParameters {
        public string Name;
        public string CP;
        public string Behavior;
        public string Appearance;
    }

    [SerializeField] private QualtricsParameters parameters;
    
    [SerializeField] private string baseUrl = "";
    
    [SerializeField] private string finalUrl = "";
    
    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q)) {
            WebViewManager webViewManager = FindObjectOfType<WebViewManager>();
            if (webViewManager != null) {
                finalUrl = GetFinalUrl();
                webViewManager.LoadURLClientRPC(finalUrl);
                
                webViewManager.ToggleWebViewVisibility();
            }
        }
    }
    
    public void SetParameters(string pairName = "", string cp = "", string behavior = "", string appearance = "") {
        if (pairName != "") {
            parameters.Name = pairName;
        }
        
        if (cp != "") {
            parameters.CP = cp;
        }
        
        if (behavior != "") {
            parameters.Behavior = behavior;
        }

        if (appearance != ""){
            parameters.Appearance = appearance;
        }
    }
    
    private string GetFinalUrl() {
        finalUrl = baseUrl + "?Name=" + parameters.Name + "&CP=" + parameters.CP + "&Behavior=" + parameters.Behavior + "&Appearance=" + parameters.Appearance;
        return finalUrl;
    }
}
