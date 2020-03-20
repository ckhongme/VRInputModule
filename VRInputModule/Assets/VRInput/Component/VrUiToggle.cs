using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace FingerInput
{
    public class VrUiToggle: Toggle
    {
        private Transform enterFinger;
        private bool isStartDrag = false;

        protected override void Start()
        {
            base.Start();
            onValueChanged.AddListener((x) => { ToggleFeedback(); });
        }

        public override void OnPointerClick(PointerEventData data)
        {
            enterFinger = ((FingerPointerEventData)data).Pointer;
            base.OnPointerClick(data);
        }

        private void ToggleFeedback()
        {
        }

        [ContextMenu("Set Toggle")]
        public void SetToggle()
        {
            targetGraphic = transform.Find("Background").GetComponent<Graphic>();
            graphic = transform.Find("Background/Checkmark").GetComponent<Graphic>();
        }
    }
}