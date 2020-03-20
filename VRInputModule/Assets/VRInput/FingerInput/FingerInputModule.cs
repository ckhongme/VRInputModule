using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FingerInput
{
    /// <summary>
    /// 创建EventSystem后，替换掉原来的 Standalone Input Module
    /// </summary>
    public class FingerInputModule : BaseInputModule
    {
        [Tooltip("only support two finger now")]
        public List<Transform> indexTfms = new List<Transform>();
        private Dictionary<int, FingerPointerEventData> m_PointerData = new Dictionary<int, FingerPointerEventData>();

        private static FingerInputModule instance;
        public static FingerInputModule Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// 在Ui前方，判定为press的距离
        /// </summary>
        public static float pressDistance = 0.01f;
        public bool Enable = true;

        /// <summary>
        /// 双击的间隔时间
        /// </summary>
        private float doubleClickTime = 0.3f;
        private float dragThreshold = 5f;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        private FingerPointerEventData m_InputPointerEvent;
        public FingerPointerEventData CurrentInputPointer { get { return m_InputPointerEvent; } }

        public override void UpdateModule()
        {
            if (!eventSystem.isFocused)
            {
                if (m_InputPointerEvent != null && m_InputPointerEvent.pointerDrag != null && m_InputPointerEvent.dragging)
                    ExecuteEvents.Execute(m_InputPointerEvent.pointerDrag, m_InputPointerEvent, ExecuteEvents.endDragHandler);
                m_InputPointerEvent = null;
                return;
            }
        }

        //called once per frame on EventSystem
        public override void Process()
        {
            if (!Enable) return;
            //是否需要选中Game窗口
            if (!eventSystem.isFocused) return;

            SendUpdateEventToSelectedObject();
            ProcessFingerEvent();
        }

        protected bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        private void ProcessFingerEvent()
        {
            for (int i = 0; i < indexTfms.Count; i++)
            {
                var pointerData = GetFingerPointerData(i);
                if (pointerData.pointerCurrentRaycast.gameObject == null)
                {
                    if (pointerData.pointerEnter != null)
                    {
                        pointerData.pressed = false;
                        HandlePointerExitAndEnter(pointerData, null);
                    }
                    if (pointerData.lastPress == null) continue;
                }

                //Debug.Log(pointerData.pointerCurrentRaycast.gameObject);
                bool released;
                PrecessPress(pointerData, out released);
                //Debug.DrawLine(indexTfm.position, raycast.worldPosition, raycast.isValid ? Color.green:Color.red);
                if (released)
                {
                    ResetPointerData(pointerData);
                }
                else
                {
                    ProcessMove(pointerData);
                    ProcessDrag(pointerData);
                }
            }
        }

        /// <summary>
        /// 获取PointerData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private FingerPointerEventData GetFingerPointerData(int id)
        {
            FingerPointerEventData pointerData;
            if (!m_PointerData.TryGetValue(id, out pointerData))
            {
                pointerData = new FingerPointerEventData(eventSystem)
                {
                    pointerId = id,
                    Pointer = indexTfms[id],                      //指定Pointer的对象
                    button = PointerEventData.InputButton.Left,   //模拟的鼠标的左键
                };
                m_PointerData.Add(id, pointerData);
            }

            if (!pointerData.isChangedData)
            {
                pointerData.position3D = pointerData.Pointer.position;
            }

            pointerData.Reset();
            eventSystem.RaycastAll(pointerData, m_RaycastResultCache);
            var raycast = FindFirstRaycast(m_RaycastResultCache);
            pointerData.pointerCurrentRaycast = raycast;
            m_RaycastResultCache.Clear();

            return pointerData;
        }

        protected void PrecessPress(FingerPointerEventData pointerEvent, out bool released)
        {
            released = false;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;
            var distance = Vector3.Distance(pointerEvent.pointerCurrentRaycast.worldPosition, pointerEvent.Pointer.position);

            var pressed = IsPress(pointerEvent, distance);    
            if (pressed)
            {
                //初始化当前 PointerEventData 的相关值
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
                pointerEvent.position3DDelta = Vector3.zero;

                // 检测是否需要删除当前 EventSystem 的 Selected 对象 (比如点击了新的对象，就会删除旧的 Selected 对象，然后由当前的 press 决定新的 Selected 对象)
                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    //send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                // 处理 pointer down 事件
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                // didnt find a press handler... search for a click handler
                // 若未能处理 press，接着寻找能处理 click 事件的对象
                if (newPressed == null)
                    newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                float time = Time.unscaledTime;

                //对 PointerEventData 相关属性赋值
                //判断是否双击
                if (newPressed == pointerEvent.lastPress)
                {
                    var diffTime = time - pointerEvent.clickTime;
                    if (diffTime < doubleClickTime)
                        ++pointerEvent.clickCount;
                    else
                        pointerEvent.clickCount = 1;
                    pointerEvent.clickTime = time;
                }
                else
                {
                    pointerEvent.clickCount = 1;
                }

                pointerEvent.pointerPress = newPressed;
                pointerEvent.rawPointerPress = currentOverGo;

                if (pointerEvent.pointerPress != null)
                {
                    pointerEvent.pressed = true;
                    //Debug.Log("press" + "  " + pointerEvent.pointerPress + "  " + Time.realtimeSinceStartup);
                }

                pointerEvent.clickTime = time;
                // Save the drag handler as well
                // 处理拖拽事件（ ExecuteEvents.initializePotentialDrag 事件）
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                {
                    pointerEvent.pressPosition = Change3Dto2D(pointerEvent.Pointer.position, pointerEvent.pointerDrag.transform);
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
                }

                m_InputPointerEvent = pointerEvent;
            }

            //pointerEvent.position3DDelta = pressed ? Vector3.zero : (pointerEvent.Pointer.position - pointerEvent.position3D);
            pointerEvent.position3DDelta = pointerEvent.Pointer.position - pointerEvent.position3D;
            pointerEvent.position3D = pointerEvent.Pointer.position;

            if (pointerEvent.pressed)
                released = IsRelease(pointerEvent, distance);    

            if (released)
            {
                //Debug.Log("released  " + pointerEvent.pointerPress + "   " + Time.realtimeSinceStartup);
                pointerEvent.pressed = false;
                //Debug.Log("Executing pressup on: " + pointer.pointerPress);
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // see if we mouse up on the same element that we clicked on...
                var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
                }
                else if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.rawPointerPress = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;

                m_InputPointerEvent = pointerEvent;
            }
        }

        private bool IsPress(FingerPointerEventData pointerEvent, float dist)
        {
            if (pointerEvent.pressed) return false;

            var direction = pointerEvent.pointerCurrentRaycast.worldPosition - pointerEvent.Pointer.position;
            var isAppendGraphic = Vector3.Dot(direction, pointerEvent.Pointer.forward) > 0;

            //Debug.Log("isAppendGraphic: " + isAppendGraphic + " dist < PressDis : " +  (dist < pressDistance));

            //满足在Ui前方，且距离Ui一定距离就符合条件，所以一开始就从ui后方向ui前方移动，也可以触发
            if (isAppendGraphic)
                return dist < pressDistance;
            else
                //return false; 
                return true;
                //return dist < 0.1f;
        }

        private bool IsRelease(FingerPointerEventData pointerEvent, float dist)
        {
            var direction = pointerEvent.pointerCurrentRaycast.worldPosition - pointerEvent.Pointer.position;
            var isAppendGraphic = Vector3.Dot(direction, pointerEvent.Pointer.forward) > 0;

            //if (isAppendGraphic && dist > pressDistance)
            //    Debug.Log("release check AppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);

            //if (!isAppendGraphic && dist > overDistance)
            //    Debug.Log("release check NoAppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);

            if (isAppendGraphic)
                return dist > pressDistance;
            else
            {
                return false;
                //return dist > overDistance;
            }

        }

        protected virtual void ProcessMove(FingerPointerEventData eventData)
        {
            var hoverGO = eventData.pointerCurrentRaycast.gameObject;
            if (eventData.pointerEnter != hoverGO)
            {
                HandlePointerExitAndEnter(eventData, hoverGO);
            }
        }

        protected virtual void ProcessDrag(FingerPointerEventData pointerEvent)
        {
            if (pointerEvent.pointerDrag == null)
                return;

            pointerEvent.position = pointerEvent.pointerPressRaycast.screenPosition;
            pointerEvent.delta = Change3Dto2D(pointerEvent.position3DDelta, pointerEvent.pointerDrag.transform);

            if (!pointerEvent.dragging && ShouldStartDrag(pointerEvent, dragThreshold))
            {
                //Debug.Log("beginDrag  " + Time.realtimeSinceStartup);
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Debug.Log("dragging  " + pointerEvent.delta);
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                    pointerEvent.rawPointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        private bool ShouldStartDrag(FingerPointerEventData eventData, float threshold)
        {
            if (!eventData.useDragThreshold)
                return true;
            return eventData.delta.sqrMagnitude >= threshold;
        }

        protected void ResetPointerData(FingerPointerEventData pointerData)
        {
            pointerData.ResetPartData();
        }

        protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }

        public override void DeactivateModule()
        {
            base.DeactivateModule();
            ClearSelection();
        }

        protected void ClearSelection()
        {
            var baseEventData = GetBaseEventData();

            foreach (var pointer in m_PointerData.Values)
            {
                // clear all selection
                HandlePointerExitAndEnter(pointer, null);
            }

            m_PointerData.Clear();
            eventSystem.SetSelectedGameObject(null, baseEventData);
        }

        // 获取 3维向量 在target 的xy平面上的 2维向量；
        public static Vector2 Change3Dto2D(Vector3 three, Transform target)
        {
            // *target.lossyScale.x
            return target.InverseTransformVector(Vector3.ProjectOnPlane(three, target.forward));
        }
    }
}
