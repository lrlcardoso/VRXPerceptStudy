using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenController : MonoBehaviour
{
    Text textUI = default;

    void Awake() 
    {
      if (textUI == null) {
        textUI = GetComponentInChildren<Text>();
      }
    }

    public void SetText(string text, int size) 
    {
      textUI.fontSize = size;
      textUI.text = text;
    }
}