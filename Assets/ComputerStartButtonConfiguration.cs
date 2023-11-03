using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ComputerStartButtonConfiguration : MonoBehaviour {

    public enum StartType {
        Server, Host, Client
    }
[SerializeField]
    public StartType ThisStartType;
    [SerializeField]
    public ParticipantOrder ThisParticipantOrder;
    [SerializeField]
    public SpawnType ThisSpawnType;
    [SerializeField]
    public JoinType ThisJoinType;

    private  static string trunc(string str,int length=5) {
        return str.Length > length ? str.Substring(0, length) : str;
    }
    // Start is called before the first frame update
    void Start() {
        
       
        
        GetComponentInChildren<Text>().text = $"{trunc(ThisStartType.ToString())}" +
                                              $"({trunc(ThisParticipantOrder.ToString())})" +
                                              $" {trunc(ThisSpawnType.ToString())}" +
                                              $"-{trunc(ThisJoinType.ToString())}";
    }

}
