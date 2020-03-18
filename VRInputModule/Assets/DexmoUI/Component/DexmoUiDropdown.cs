using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Dexmo.Unity.Interaction;
using Dexmo.Unity;

namespace Dexmo.UI
{
    /// <summary>
    /// the sizeDelta of canvas should include all items while using the Dropdown component; 
    /// </summary>
    public class DexmoUiDropdown : Dropdown
    {
        private GameObject _dropdown;
        private Transform enterFinger;
        private bool isStartDrag = false;

        protected override void Start()
        {
            onValueChanged.AddListener((i) => { DexmoUiInputModule.StartUiFeedback(enterFinger); });
        }

        // 给创建的 DropdownList 也增加 DexmoUiRaycaster；
        protected override GameObject CreateDropdownList(GameObject template)
        {
            _dropdown = (GameObject)Instantiate(template);
            _dropdown.AddComponent<DexmoUiRaycaster>();
            return _dropdown;
        }

        // 默认是 blocker 上添加的是 GraphicRaycaster，这里需要改成 DexmoUiRaycaster;
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
            blocker.AddComponent<DexmoUiRaycaster>();

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
            enterFinger = ((Pointer3DEventData)eventData).Pointer;
            base.OnPointerClick(eventData);
        }

        [ContextMenu("Set Dropdown")]
        public void SetDropdown()
        {
            template = transform.Find("Template").GetComponent<RectTransform>();
            captionText = transform.Find("Label").GetComponent<Text>();
            itemText = template.transform.Find("Viewport/Content/Item/Item Label").GetComponent<Text>();
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetDropdown();
        }
#endif
    }
}