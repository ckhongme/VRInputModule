using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FingerInput
{
    public class FingerPointerEventData : PointerEventData
    {
        public Transform Pointer;
        public Vector3 position3D;
        public Vector3 position3DDelta;
        public bool pressed;
        public bool ignoreReversedGraphics;
        public bool isChangedData;

        //public Quaternion rotationDelta;
        //public Vector3 pressPosition3D;
        //public Quaternion pressRotation3D;
        //public GameObject pressEnter;

        public FingerPointerEventData(EventSystem eventSystem) : base(eventSystem)
        {
            isChangedData = false;
        }

        public override void Reset()
        {
            base.Reset();
            isChangedData = true;
        }

        public void ResetPartData()
        {
            pointerEnter = null;
            rawPointerPress = null;
            pointerDrag = null;
            pointerCurrentRaycast = new RaycastResult();
            pointerPressRaycast = new RaycastResult();

            position = Vector2.zero;
            delta = Vector2.zero;
            pressPosition = Vector2.zero;
            clickTime = 0;
            clickCount = 0;

            scrollDelta = Vector2.zero;
            useDragThreshold = false;
            dragging = false;

            position3D = Vector3.zero;
            position3DDelta = Vector3.zero;

            //pressEnter = null;
            pressed = false;
            isChangedData = false;
        }
    }
}