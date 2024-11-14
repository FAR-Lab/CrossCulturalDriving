using TLab.Android.WebView;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WebViewManager : NetworkBehaviour
{
    [SerializeField] private TLabWebView m_webView;
    [SerializeField] private bool isWebViewEnabled = false;
    
    private GameObject m_webViewObject;
    
    void Start()
    {
        m_webViewObject = m_webView.gameObject;
        
        if (!IsServer) {
            StartWebView();
        }

        if (IsServer) {
            SetWebViewEnabled(isWebViewEnabled);
        }
    }
    
    public void StartWebView()
    {
        m_webView.Init();
    }

    void Update()
    {
        if (IsServer) {
            HandleServerActions();
        }
        
        
#if UNITY_ANDROID
        m_webView.UpdateFrame();
#endif
    }

    private void HandleServerActions() {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Q)) {
            isWebViewEnabled = !isWebViewEnabled;
            SetWebViewEnabled(isWebViewEnabled);
        }
        
    }

    private void SetWebViewEnabled(bool enable) {
        if (!IsServer) return;
        
        SetWebViewEnabledInternal(enable);
        SetWebViewEnabledClientRPC(enable);
    }

    private void SetWebViewEnabledInternal(bool enable) {
        RawImage rawImage = m_webView.GetComponent<RawImage>();
        rawImage.enabled = enable;
    }
    
    [ClientRpc]
    private void SetWebViewEnabledClientRPC(bool enable) {
        SetWebViewEnabledInternal(enable);
    }
    
    [ClientRpc]
    public void LoadURLClientRPC(string url) {
        m_webView.LoadUrl(url);
    }
}


