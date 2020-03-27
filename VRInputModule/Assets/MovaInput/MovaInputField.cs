using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MovaInputField : InputField
{
    private bool isActive = false;

    public override void OnSelect(BaseEventData eventData)
    {
        base.OnSelect(eventData);
        isActive = true;
        MovaKeyboard.keyboard.OnKeyInput += OnKeyboardCharInput;
    }

    public override void OnDeselect(BaseEventData eventData)
    {
        base.OnDeselect(eventData);
        isActive = false;
        MovaKeyboard.keyboard.OnKeyInput -= OnKeyboardCharInput;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        isActive = false;
    }

    private void OnKeyboardCharInput(object e, string input)
    {
        if (!isActive) return;

        switch (input)
        {
            case "Backspace":
                if (text.Length > 0)
                {
                    text = text.Substring(0, text.Length - 1);
                }
                break;
            default:
                text += input;
                MoveTextEnd(false);
                break;
        }  
    }
}
