using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FingerInput
{
    public class VrUiSlider : Slider
    {
        public override void OnPointerEnter(PointerEventData data)
        {
            var enterTarget = ((FingerPointerEventData)data).Pointer;
            SliderEnterFeedback(enterTarget);
            base.OnPointerEnter(data);
        }

        public override void OnPointerExit(PointerEventData data)
        {
            var exitTarget = ((FingerPointerEventData)data).Pointer;
            SliderExitFeedback(exitTarget);
            base.OnPointerExit(data);
        }

        protected virtual void SliderEnterFeedback(Transform enterTarget)
        {

        }

        protected virtual void SliderExitFeedback(Transform enterTarget)
        {

        }

        [ContextMenu("Set Slider")]
        public void SetSlider()
        {
            targetGraphic = transform.Find("Handle Slide Area/Handle").GetComponent<Graphic>();
            fillRect = transform.Find("Fill Area/Fill").GetComponent<RectTransform>();
            handleRect = transform.Find("Handle Slide Area/Handle").GetComponent<RectTransform>();
        }
    }
}
