using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Dexmo.Unity.Interaction;
using Dexmo.Unity;

namespace Dexmo.UI
{
    public class DexmoUiSlider : Slider
    {
        private Transform enterFinger;
        private bool isStartDrag = false;

        public override void OnPointerEnter(PointerEventData data)
        {
            enterFinger = ((Pointer3DEventData)data).Pointer;
            DexmoUiInputModule.StartUiFeedback(enterFinger);
            base.OnPointerEnter(data);
        }

        public override void OnPointerExit(PointerEventData data)
        {
            enterFinger = ((Pointer3DEventData)data).Pointer;
            DexmoUiInputModule.StopUiFeedback(enterFinger);
            base.OnPointerExit(data);
        }

        [ContextMenu("Set Slider")]
        public void SetSlider()
        {
            targetGraphic = transform.Find("Handle Slide Area/Handle").GetComponent<Graphic>();
            fillRect = transform.Find("Fill Area/Fill").GetComponent<RectTransform>();
            handleRect = transform.Find("Handle Slide Area/Handle").GetComponent<RectTransform>();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetSlider();
        }
#endif
    }
}