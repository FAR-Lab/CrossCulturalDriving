using TLab.Android.WebView;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WebViewManager : NetworkBehaviour
{
    [SerializeField] private TLabWebView m_webView;
    [SerializeField] private NetworkVariable<bool> isWebViewEnabled = new NetworkVariable<bool>(true);

    [SerializeField] private string url = "";
    
    private GameObject m_webViewObject;
    
    void Start()
    {
        m_webViewObject = m_webView.gameObject;
        
        if (IsServer) {
            StartWebView();
        }
        SetWebViewEnabled();
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
            isWebViewEnabled.Value = !isWebViewEnabled.Value;
            SetWebViewEnabled();
        }
        
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.L)) {
            LoadURLClientRPC(url);
        }
    }

    private void SetWebViewEnabled() {
        if (!IsServer) return;
        
        SetWebViewEnabledInternal();
        SetWebViewEnabledClientRPC();
    }

    private void SetWebViewEnabledInternal() {
        RawImage rawImage = m_webView.GetComponent<RawImage>();
        rawImage.enabled = isWebViewEnabled.Value;
    }
    
    [ClientRpc]
    private void SetWebViewEnabledClientRPC() {
        SetWebViewEnabledInternal();
    }
    
    [ClientRpc]
    private void LoadURLClientRPC(string url) {
        m_webView.LoadUrl(url);
    }
}


