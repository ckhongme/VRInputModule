 using Dexmo.Unity;
using Dexmo.Unity.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dexmo.UI
{
    public class DexmoUiBtn : Button
    {
        private Transform enterFinger;
        public bool autoSetColor = true;

        protected override void Start()
        {
            onClick.AddListener(() => { DexmoUiInputModule.StartUiFeedback(enterFinger); });
        }

        public override void OnPointerClick(PointerEventData data)
        {
            enterFinger = ((Pointer3DEventData)data).Pointer;
            base.OnPointerClick(data);
        }
    }
}