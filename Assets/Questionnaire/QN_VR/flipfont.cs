using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
 //From https://answers.unity.com/questions/1034235/how-to-write-text-from-left-to-right.html
 
 public class flipfont : MonoBehaviour {

    Text myText; //You can also make this public and attach your UI text here.

    string individualLine = ""; //Control individual line in the multi-line text component.

    int numberOfAlphabetsInSingleLine = 20;

    string sampleString = "זה רק מבחן.";

    void Awake() {
        myText = GetComponent<Text>();
    }

    void Start() {

        myText.text = Reverse(myText.text);
    }

    public static string Reverse(string s) {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

}