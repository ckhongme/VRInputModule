using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovaKeyboard : MonoBehaviour
{
    public static bool isAutoHide = false;
    private static bool isAppQuit = false;

    private static MovaKeyboard _keyboard;
    public static MovaKeyboard keyboard
    {
        get { return _keyboard == null ? CreateKeyboard() : _keyboard; }
    }

    private static MovaKeyboard CreateKeyboard()
    {
        if (isAppQuit) return null;

        var keyboardObj = new GameObject("MovaKeyboard");
        _keyboard = keyboardObj.AddComponent<MovaKeyboard>();
        return _keyboard;
    }

    private static System.Text.StringBuilder strBuilder;

    public event EventHandler<string> OnKeyInput;

    public void ShowKeyboard()
    {
        if (!isAutoHide) return;
    }

    public void HideKeyboard()
    {
        if (!isAutoHide) return;
    }

    public void KeyInput(string input)
    {
        if (OnKeyInput != null)
            OnKeyInput.Invoke(this, input);
    }

    public void OnDestroy()
    {
        isAppQuit = true;
        _keyboard = null;
    }
}
