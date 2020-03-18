using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Dexmo.Unity.Interaction;
using Dexmo.Unity;

namespace Dexmo.UI
{
    public class DexmoUiToggle : Toggle
    {
        private Transform enterFinger;
        protected ColorBlock cb;
        private bool isStartDrag = false;

        protected override void Start()
        {
            onValueChanged.AddListener((x) => { DexmoUiInputModule.StartUiFeedback(enterFinger); });
        }

        public override void OnPointerClick(PointerEventData data)
        {
            enterFinger = ((Pointer3DEventData)data).Pointer;
            base.OnPointerClick(data);
        }

        [ContextMenu("Set Toggle")]
        public void SetToggle()
        {
            targetGraphic = transform.Find("Background").GetComponent<Graphic>();
            graphic = transform.Find("Background/Checkmark").GetComponent<Graphic>();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetToggle();
        }
#endif
    }
}