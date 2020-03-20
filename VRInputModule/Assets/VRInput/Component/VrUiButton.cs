using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FingerInput
{
    public class VrUiButton : Button
    {
        private Transform enterFinger;

        protected override void Start()
        {
            onClick.AddListener(() => { BtnFeedback(); });
        }

        public override void OnPointerClick(PointerEventData data)
        {
            enterFinger = ((FingerPointerEventData)data).Pointer;
            base.OnPointerClick(data);
        }

        private void BtnFeedback()
        {

        }
    }
}