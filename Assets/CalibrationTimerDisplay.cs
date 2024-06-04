using System.Collections;
using TMPro;
using UnityEngine;

public class CalibrationTimerDisplay : MonoBehaviour {
    public const float TurnOfDelay = 1f;

    private TextMeshProUGUI Text;

    // Start is called before the first frame update
    private void Start() {
        Debug.Log(transform.Find("timer"));
        Text = transform.Find("timer").GetComponent<TextMeshProUGUI>();
        Text.text = "";
        SetActive(false);
    }


    public void StartDisplay() {
        SetActive(true);
        Text.color = Color.red;
    }

    private void SetActive(bool val) {
        gameObject.SetActive(val);
    }

    public void UpdateMessage(string toString) {
        Text.text = toString;
    }

    public void StopDisplay() {
        if (gameObject.activeSelf) {
            Text.text = "Done!";
            Text.color = Color.green;
            StartCoroutine(StopDisplayCoroutine());
        }
    }

    private IEnumerator StopDisplayCoroutine() {
        yield return new WaitForSeconds(TurnOfDelay);
        Text.text = "";
        SetActive(false);
    }
}