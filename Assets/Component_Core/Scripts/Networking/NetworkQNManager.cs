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
    }

    [SerializeField] private QualtricsParameters parameters;
    
    [SerializeField] private string baseUrl = "";
    
    [SerializeField] private string finalUrl = "";
    
    private void Update() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.L)) {
            WebViewManager webViewManager = FindObjectOfType<WebViewManager>();
            if (webViewManager != null) {
                finalUrl = GetFinalUrl();
                webViewManager.LoadURLClientRPC(finalUrl);
            }
        }
    }
    
    public void SetParameters(string pairName = "", string cp = "", string behavior = "") {
        if (pairName != "") {
            parameters.Name = pairName;
        }
        
        if (cp != "") {
            parameters.CP = cp;
        }
        
        if (behavior != "") {
            parameters.Behavior = behavior;
        }
    }
    
    private string GetFinalUrl() {
        finalUrl = baseUrl + "?Name=" + parameters.Name + "&CP=" + parameters.CP + "&Behavior=" + parameters.Behavior;
        return finalUrl;
    }
}
