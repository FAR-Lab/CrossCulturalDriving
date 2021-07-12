using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision) {
        SceneManager.LoadScene("Survey");
    }
}
