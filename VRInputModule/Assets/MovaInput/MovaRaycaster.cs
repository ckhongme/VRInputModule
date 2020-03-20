using HTC.UnityPlugin.Pointer3D;
using HTC.UnityPlugin.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;


namespace Mova
{
    public class MovaRaycaster : Pointer3DRaycaster
    {
        [SerializeField]
        [FormerlySerializedAs("m_mouseBtnLeft")]
        [CustomOrderedEnum]
        private ControllerButton m_mouseButtonLeft = ControllerButton.Trigger;

        [SerializeField]
        [FormerlySerializedAs("m_buttonEvents")]
        [FlagsFromEnum(typeof(ControllerButton))]
        private ulong m_additionalButtons = 0ul;
        [SerializeField]
        private ScrollType m_scrollType = ScrollType.Auto;
        [SerializeField]
        private Vector2 m_scrollDeltaScale = new Vector2(1f, -1f);

        public ControllerButton mouseButtonLeft { get { return m_mouseButtonLeft; } }

        public ulong additionalButtonMask { get { return m_additionalButtons; } }
        public ScrollType scrollType { get { return m_scrollType; } set { m_scrollType = value; } }
        public Vector2 scrollDeltaScale { get { return m_scrollDeltaScale; } set { m_scrollDeltaScale = value; } }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            FilterOutAssignedButton();
            AddGuideLineDrawer();
        }
#endif
        protected void FilterOutAssignedButton()
        {
            if (EnumUtils.GetFlag(m_additionalButtons, (int)m_mouseButtonLeft)) { EnumUtils.SetFlag(ref m_additionalButtons, (int)m_mouseButtonLeft, false); }
        }

        protected void AddGuideLineDrawer()
        {
            var lineDrawer = GetComponentInChildren<RayDrawer>();
            if (lineDrawer == null)
            {
                GameObject drawer = new GameObject("RayDrawer");
                drawer.transform.SetParent(this.transform);
                drawer.transform.localPosition = Vector3.zero;
                drawer.transform.localRotation = Quaternion.identity;

                drawer.AddComponent<RayDrawer>();
            }
        }

        protected override void Start()
        {
            base.Start();

            buttonEventDataList.Add(new MovaPointerEventData(this, EventSystem.current, m_mouseButtonLeft, PointerEventData.InputButton.Left));

            FilterOutAssignedButton();

            var mouseBtn = PointerEventData.InputButton.Middle + 1;
            var addBtns = m_additionalButtons;
            for (ControllerButton btn = 0; addBtns > 0u; ++btn, addBtns >>= 1)
            {
                if ((addBtns & 1u) == 0u) { continue; }

                buttonEventDataList.Add(new MovaPointerEventData(this, EventSystem.current, btn, mouseBtn++));
            }
        }

        public override Vector2 GetScrollDelta()
        {
            return MovaInput.GetScrollDelta(m_scrollType, m_scrollDeltaScale);
        }
    }
}