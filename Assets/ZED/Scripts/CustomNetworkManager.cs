using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode.Transports.UTP;


public class CustomNetworkManager : MonoBehaviour
{
    public Button serverButton;
    public Button clientButton;
    public GameObject fusionManagerPrefab;
    public TMP_InputField ipInputField;
    public GameObject Managers;
    private void Start()
    {
        serverButton.onClick.AddListener(StartServer);
        clientButton.onClick.AddListener(StartClient);
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        serverButton.GetComponentInChildren<TextMeshProUGUI>().text = "I'm server";
        // instantiate fusion manager under Managers
        Instantiate(fusionManagerPrefab, Managers.transform);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();

        if(ipInputField.text == ""){
            // do nothing
        }
        else{
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipInputField.text;
        }


        clientButton.GetComponentInChildren<TextMeshProUGUI>().text = "I'm client";
    }
}
