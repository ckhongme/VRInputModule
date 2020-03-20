using HTC.UnityPlugin.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    public class RawControllerState
    {
        public readonly bool[] buttonPress = new bool[MovaInput.CONTROLLER_BUTTON_COUNT];
        public readonly float[] axisValue = new float[MovaInput.CONTROLLER_AXIS_COUNT];
    }

    public class MovaInput : SingletonBehaviour<MovaInput>
    {
        public static readonly int CONTROLLER_BUTTON_COUNT = EnumUtils.GetMaxValue(typeof(ControllerButton)) + 1;
        public static readonly int CONTROLLER_AXIS_COUNT = EnumUtils.GetMaxValue(typeof(ControllerAxis)) + 1;
        public static readonly int BUTTON_EVENT_COUNT = EnumUtils.GetMaxValue(typeof(ButtonEventType)) + 1;

        [SerializeField]
        private float m_clickInterval = 0.3f;
        [SerializeField]
        private bool m_dontDestroyOnLoad = false;
        [SerializeField]
        private UnityEvent m_onUpdate = new UnityEvent();

        public static float clickInterval
        {
            get { return Instance.m_clickInterval; }
            set { Instance.m_clickInterval = Mathf.Max(0f, value); }
        }

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

        public static bool GetPressUp(ControllerButton movaButton)
        {
            switch (movaButton)
            {
                case ControllerButton.Menu: return Input.GetKeyUp(KeyCode.M);
                case ControllerButton.Trigger: return Input.GetKeyUp(KeyCode.T);
                case ControllerButton.Pad: return Input.GetKeyUp(KeyCode.D);
            }
            return false;
        }

        public static bool GetPressDown(ControllerButton movaButton)
        {
            switch (movaButton)
            {
                case ControllerButton.Menu: return Input.GetKeyDown(KeyCode.M);
                case ControllerButton.Trigger: return Input.GetKeyDown(KeyCode.T);
                case ControllerButton.Pad: return Input.GetKeyDown(KeyCode.D);
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            m_clickInterval = Mathf.Max(m_clickInterval, 0f);
        }
#endif
        protected override void OnSingletonBehaviourInitialized()
        {
            if (m_dontDestroyOnLoad && transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private static bool IsValidButton(ControllerButton button) { return button >= 0 && (int)button < CONTROLLER_BUTTON_COUNT; }

        private static bool IsValidAxis(ControllerAxis axis) { return axis >= 0 && (int)axis < CONTROLLER_BUTTON_COUNT; }
    }
}