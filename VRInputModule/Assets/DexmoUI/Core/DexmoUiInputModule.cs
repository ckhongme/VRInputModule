/* 
 * 目前没有Editor模式下的运行脚本，所以要运行 DexmoUI的脚本，需要在运行中选中Game视窗；
 */

using Dexmo.Unity;
using Dexmo.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dexmo.UI
{
    /// <summary>
    /// 创建EventSystem后，替换掉原来的 Standalone Input Module
    /// </summary>
    public class DexmoUiInputModule : BaseInputModule
    {
        [Tooltip("only support two finger now")]
        public List<Transform> indexTfms = new List<Transform>();
        private Dictionary<int, Pointer3DEventData> m_PointerData = new Dictionary<int, Pointer3DEventData>();

        private static DexmoUiInputModule instance;
        public static DexmoUiInputModule Instance
        {
            get { return instance; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static float pressDistance = 0.01f;
        public static float overDistance = 0.1f;
        public static float exitDistance = 0.025f;
       
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

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            SetIndex();
        }
#endif

        [ContextMenu("Setting InputModule")]
        public void SetIndex()
        {
            indexTfms.Clear();
            var hands = GameObject.FindObjectsOfType<InteractionHand>();
            Transform index = null;
            foreach (var item in hands)
            {
                var palm = item.transform.GetChild(0).GetChild(0);
                for (int i = 0; i < palm.childCount; i++)
                {
                    if (palm.GetChild(i).name.Contains("index"))
                    {
                        index = palm.GetChild(i);
                        break;
                    }
                    else
                        index = null;
                }

                if (index != null)
                    indexTfms.Add(index.GetChild(0).GetChild(0).GetChild(0));
            }
        }

        private Pointer3DEventData m_InputPointerEvent;
        public Pointer3DEventData CurrentInputPointer { get { return m_InputPointerEvent; } }

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
            if (!eventSystem.isFocused) return;

            SendUpdateEventToSelectedObject();
            ProcessDexmoEvent();
        }

        protected bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        private void ProcessDexmoEvent()
        {
            for (int i = 0; i < indexTfms.Count; i++)
            {
                var pointerData = GetDexmoPointerData(i);
                if (pointerData.pointerCurrentRaycast.gameObject == null)
                {
                    if (pointerData.pointerEnter != null)
                    {
                        //
                        pointerData.pressPrecessed = false;
                        HandlePointerExitAndEnter(pointerData, null);
                    }
                    if (pointerData.lastPress == null) continue;
                }

                //Debug.Log(pointerData.pointerCurrentRaycast.gameObject);
                bool released;
                PrecessPress(pointerData, out released);
                //Debug.DrawLine(indexTfm.position, raycast.worldPosition, raycast.isValid ? Color.green:Color.red);
                if (!released)
                {
                    ProcessMove(pointerData);
                    ProcessDrag(pointerData);
                }
                else
                {
                    ResetPointerData(pointerData);
                }
            }
        }

        private Pointer3DEventData GetDexmoPointerData(int id)
        {
            Pointer3DEventData pointerData;
            if (!m_PointerData.TryGetValue(id, out pointerData))
            {
                pointerData = new Pointer3DEventData(eventSystem)
                {
                    pointerId = id,
                    Pointer = indexTfms[id],
                    button = PointerEventData.InputButton.Left,
                };
                m_PointerData.Add(id, pointerData);
            }

         //   Debug.Log(pointerData.pointerId);

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

        protected void PrecessPress(Pointer3DEventData pointerEvent, out bool released)
        {
            released = false;
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;
            var distance = Vector3.Distance(pointerEvent.pointerCurrentRaycast.worldPosition, pointerEvent.Pointer.position);

            var pressed = IsPress(pointerEvent, distance);
            pointerEvent.position3DDelta = pressed ? Vector3.zero : (pointerEvent.Pointer.position - pointerEvent.position3D);

            if (pressed)
            {
                //初始化当前 PointerEventData 的相关值
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;
                pointerEvent.pressPosition3D = pointerEvent.Pointer.position;

                // 检测是否需要删除当前 EventSystem 的 Selected 对象 (比如点击了新的对象，就会删除旧的 Selected 对象，然后由当前的 press 决定新的 Selected 对象)
                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
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

                // 得到 newPressed 对象之后，接着对 PointerEventData 相关属性赋值
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
                    pointerEvent.pressPrecessed = true;
                    //Debug.Log("press" + "  " + pointerEvent.pointerPress + "  " + Time.realtimeSinceStartup);
                }

                pointerEvent.clickTime = time;
                // Save the drag handler as well
                // 处理 ExecuteEvents.initializePotentialDrag 事件
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                {
                    pointerEvent.pressPosition = Change3Dto2D(pointerEvent.Pointer.position, pointerEvent.pointerDrag.transform);
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
                }

                m_InputPointerEvent = pointerEvent;
            }

            pointerEvent.position3D = pointerEvent.Pointer.position;

            if (pointerEvent.pressPrecessed)
                released = IsRelease(pointerEvent, distance);

            if (released)
            {
                //Debug.Log("released  " + pointerEvent.pointerPress + "   " + Time.realtimeSinceStartup);
                pointerEvent.pressPrecessed = false;
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

        private bool IsPress(Pointer3DEventData pointerEvent, float dist)
        {
            if (pointerEvent.pressPrecessed) return false;

            var direction = pointerEvent.pointerCurrentRaycast.worldPosition - pointerEvent.Pointer.position;
            var isAppendGraphic = Vector3.Dot(direction, pointerEvent.Pointer.forward) > 0;

            //if (isAppendGraphic && dist < pressDistance)
            //    Debug.Log("press check AppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);

            //if (!isAppendGraphic && dist < overDistance)
            //    Debug.Log("press check NoAppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);

            if (isAppendGraphic)
                return dist < pressDistance;
            else
                return dist < overDistance;
        }

        private bool IsRelease(Pointer3DEventData pointerEvent, float dist)
        {
            if (!pointerEvent.pressPrecessed) return false;

            var direction = pointerEvent.pointerCurrentRaycast.worldPosition - pointerEvent.Pointer.position;
            var isAppendGraphic = Vector3.Dot(direction, pointerEvent.Pointer.forward) > 0;

            //if (isAppendGraphic && dist > exitDistance)
            //    Debug.Log("release check AppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);

            //if (!isAppendGraphic && dist > overDistance)
            //    Debug.Log("release check NoAppendGraphic: " + dist + "  " + Time.realtimeSinceStartup);


            if (isAppendGraphic)
                return dist > exitDistance;
            else
                return dist > overDistance;

        }

        protected virtual void ProcessMove(Pointer3DEventData eventData)
        {
            var hoverGO = eventData.pointerCurrentRaycast.gameObject;
            if (eventData.pointerEnter != hoverGO)
            {
                HandlePointerExitAndEnter(eventData, hoverGO);
            }
        }

        protected virtual void ProcessDrag(Pointer3DEventData pointerEvent)
        {
            if (pointerEvent.pointerDrag == null)
                return;

            pointerEvent.position = pointerEvent.pointerPressRaycast.screenPosition;
            //
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

        private bool ShouldStartDrag(Pointer3DEventData eventData, float threshold)
        {
            if (!eventData.useDragThreshold)
                return true;
            return eventData.delta.sqrMagnitude >= threshold;
        }

        protected void ResetPointerData(Pointer3DEventData pointerData)
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

        /// <summary>
        /// 获取 3维向量 在target的xy平面上的 2维向量；
        /// </summary>
        private Vector2 Change3Dto2D(Vector3 three, Transform target)
        {
            return target.InverseTransformVector(Vector3.ProjectOnPlane(three, target.forward));
        }


        /// <summary>
        /// 启动UI振动
        /// </summary>
        /// <param name="enterFinger"></param>
        /// <param name="waveForm"></param>
        /// <param name="cycle"></param>
        /// <param name="intensity"></param>
        public static void StartUiFeedback(Transform enterFinger, Waveform waveForm = Waveform.Button, VibrationCycle cycle = VibrationCycle.Once, float intensity = 0.5f)
        {
            if (enterFinger != null && enterFinger.parent != null)
            {
                var info = enterFinger.parent.GetComponent<VibrationInfo>();
                if (info != null)
                {
                    FeedbackBridge.Instance.SetVibrationFeedback(info.hand, info.pos, Waveform.Button, VibrationCycle.Once, 0.5f, true, true);
                    FeedbackBridge.Instance.StartHandFeedback(info.hand, true, FeedBackType.VIBRATION);
                }
            }
        }

        /// <summary>
        /// 停止UI振动
        /// </summary>
        /// <param name="enterFinger"></param>
        public static void StopUiFeedback(Transform enterFinger)
        {
            if (enterFinger != null && enterFinger.parent != null)
            {
                var info = enterFinger.parent.GetComponent<VibrationInfo>();
                if (info != null)
                {
                    FeedbackBridge.Instance.StopHandFeedback(info.hand, FeedBackType.VIBRATION);
                }
            }
        }
    }
}
