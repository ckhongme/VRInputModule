using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class test : MonoBehaviour
{
    public InputField addressInput;

    // Update is called once per frame
    void Update ()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            MovaKeyboard.keyboard.KeyInput("Backspace");
        }
        else if (Input.anyKeyDown)
        {
            MovaKeyboard.keyboard.KeyInput(Input.inputString);
        }

	}
}
