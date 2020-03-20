using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FingerInput
{
    /// <summary>
    /// the sizeDelta of canvas should include all items while using the Dropdown component; 
    /// </summary>
    public class VrUiDropdown : Dropdown
    {
        private GameObject _dropdown;
        private Transform enterFinger;

        protected override void Start()
        {
            onValueChanged.AddListener((i) => { DropdownFeedback(); });
        }

        protected override GameObject CreateDropdownList(GameObject template)
        {
            _dropdown = (GameObject)Instantiate(template);
            _dropdown.AddComponent<FingerRaycaster>();
            DropdownFeedback();
            return _dropdown;
        }

        protected override GameObject CreateBlocker(Canvas rootCanvas)
        {
            // Create blocker GameObject.
            GameObject blocker = new GameObject("Blocker");

            // Setup blocker RectTransform to cover entire root canvas area.
            RectTransform blockerRect = blocker.AddComponent<RectTransform>();
            blockerRect.SetParent(rootCanvas.transform, false);
            blockerRect.anchorMin = Vector3.zero;
            blockerRect.anchorMax = Vector3.one;
            blockerRect.sizeDelta = Vector2.zero;

            // Make blocker be in separate canvas in same layer as dropdown and in layer just below it.
            Canvas blockerCanvas = blocker.AddComponent<Canvas>();
            blockerCanvas.overrideSorting = true;
            Canvas dropdownCanvas = _dropdown.GetComponent<Canvas>();
            blockerCanvas.sortingLayerID = dropdownCanvas.sortingLayerID;
            blockerCanvas.sortingOrder = dropdownCanvas.sortingOrder - 1;

            // Add raycaster since it's needed to block.
            blocker.AddComponent<FingerRaycaster>();

            // Add image since it's needed to block, but make it clear.
            Image blockerImage = blocker.AddComponent<Image>();
            blockerImage.color = Color.clear;

            // Add button since it's needed to block, and to close the dropdown when blocking area is clicked.
            Button blockerButton = blocker.AddComponent<Button>();
            blockerButton.onClick.AddListener(Hide);

            return blocker;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            enterFinger = ((FingerPointerEventData)eventData).Pointer;
            SelectFeedback();
            base.OnPointerClick(eventData);
        }

        private void DropdownFeedback()
        {
        }

        private void SelectFeedback()
        {

        }

        [ContextMenu("Set Dropdown")]
        public void SetDropdown()
        {
            template = transform.Find("Template").GetComponent<RectTransform>();
            captionText = transform.Find("Label").GetComponent<Text>();
            itemText = template.transform.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
        }
    }
}