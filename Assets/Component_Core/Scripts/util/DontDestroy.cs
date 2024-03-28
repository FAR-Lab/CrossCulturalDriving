using UnityEngine;

public class DontDestroy : MonoBehaviour {
    private void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    // Use this for initialization
    private void Start() {
    }

    // Update is called once per frame
    private void Update() {
    }
}