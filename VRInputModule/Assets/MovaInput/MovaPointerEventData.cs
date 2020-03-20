using HTC.UnityPlugin.Pointer3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Mova
{
    public class MovaPointerEventData : Pointer3DEventData
    {
        public MovaRaycaster movaRaycaster { get; private set; }
        public ControllerButton movaButton { get; private set; }

        public MovaPointerEventData(MovaRaycaster ownerRaycaster, EventSystem eventSystem, ControllerButton handleButton, InputButton mouseButton) : base(ownerRaycaster, eventSystem)
        {
            this.movaRaycaster = ownerRaycaster;
            this.movaButton = handleButton;
            this.button = mouseButton;
        }

        public override bool GetPress() { return MovaInput.GetPress(movaButton); }

        public override bool GetPressDown() { return MovaInput.GetPressDown(movaButton); }

        public override bool GetPressUp() { return MovaInput.GetPressUp(movaButton); }
    }
}