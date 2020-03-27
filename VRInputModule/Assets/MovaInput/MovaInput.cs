using HTC.UnityPlugin.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Mova
{
    public enum ButtonEventType
    {
        /// <summary>
        /// Button unpressed at last frame, pressed at this frame
        /// </summary>
        Down,
        /// <summary>
        /// Button pressed at this frame
        /// </summary>
        Press,
        /// <summary> 
        /// Button pressed at last frame, unpressed at the frame
        /// </summary>
        Up,
        /// <summary>
        /// Button up at this frame, and last button down time is in certain interval
        /// </summary>
        Click,
    }

    public enum ControllerButton
    {
        None = -1,
        Menu,
        Trigger,
        Pad,
    }

    public enum ControllerAxis
    {
        None = -1,
        PadX,
        PadY,
    }

    public enum ScrollType
    {
        None = -1,
        Auto,
        Trackpad,
    }

    public class MovaInput : BaseInput
    {
        public static Vector2 GetScrollDelta(ScrollType m_scrollType, Vector2 m_scrollDeltaScale)
        {
            //返回手柄的Scrolldelta值;
            return Vector2.zero;
        }
        public static bool GetPress(ControllerButton movaButton)
        {
            switch (movaButton)
            {
                case ControllerButton.Menu: return Input.GetKey(KeyCode.M);
                case ControllerButton.Trigger: return Input.GetKey(KeyCode.T);
                case ControllerButton.Pad:return Input.GetKey(KeyCode.D);
            }
            return false;
        }
    }
}