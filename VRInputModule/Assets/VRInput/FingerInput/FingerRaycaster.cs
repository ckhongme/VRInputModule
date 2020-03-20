using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FingerInput
{
    /// <summary>
    /// 只对WorldSpace模式的Canvas适用，
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class FingerRaycaster : BaseRaycaster
    {
        /// <summary>
        /// 在ui前方，判定为射线移入的距离
        /// </summary>
        private float rayEnterDist = 0.035f; //0.2f;  //0.035f
        /// <summary>
        /// 沿着指尖，向后延申的距离，当指尖在Ui后方，超出这个距离时，表示失效
        /// </summary>
        private float reversedRayDist = 0.1f; 

        [NonSerialized]
        private Camera fallbackCam;
        public override Camera eventCamera
        {
            get
            {
                if (fallbackCam == null)
                {
                    var go = new GameObject(name + " FallbackCamera");
                    go.SetActive(false);
                    // place fallback camera at root to preserve world position
                    go.transform.SetParent(EventSystem.current.transform, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    fallbackCam = go.AddComponent<Camera>();
                    fallbackCam.clearFlags = CameraClearFlags.Nothing;
                    fallbackCam.cullingMask = 0;
                    fallbackCam.orthographic = true;
                    fallbackCam.orthographicSize = 1;
                    fallbackCam.useOcclusionCulling = false;
#if !(UNITY_5_3 || UNITY_5_2 || UNITY_5_1 || UNITY_5_0)
                    fallbackCam.stereoTargetEye = StereoTargetEyeMask.None;
#endif
                    fallbackCam.nearClipPlane = 0;
                    fallbackCam.farClipPlane = rayEnterDist;
                }
                return fallbackCam;
            }
        }

        public bool ignoreReversedGraphics = true;
        [FormerlySerializedAs("blockingObjects")]

        private Canvas m_Canvas;
        private Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        protected override void Start()
        {
            base.Start();
            rayEnterDist = FingerInputModule.pressDistance + 0.025f;
        }

        [NonSerialized] private List<Graphic> m_RaycastResults = new List<Graphic>();
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null) return; 
            if (canvas.renderMode != RenderMode.WorldSpace) return;
            //获取Canvas下的所有图像
            var canvasGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            if (canvasGraphics == null || canvasGraphics.Count == 0)
                return;
   
            var data = (FingerPointerEventData)eventData;

            var rayOrigin = data.Pointer.position - data.Pointer.forward * reversedRayDist;
            Ray ray = new Ray(rayOrigin, data.Pointer.forward);
            //Debug.DrawRay(rayOrigin, data.Pointer.forward);
            eventCamera.transform.position = ray.origin;
            eventCamera.transform.rotation = Quaternion.LookRotation(ray.direction, transform.up);
            //eventCamera.transform.rotation = Quaternion.LookRotation(ray.direction, data.Pointer.up);        

            m_RaycastResults.Clear();

            var screenCenterPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            for (int i = 0; i < canvasGraphics.Count; ++i)
            {
                var graphic = canvasGraphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget) continue;

                // 是否禁止在ui后方触发
                if (ignoreReversedGraphics && Vector3.Dot(ray.direction, graphic.transform.forward) <= 0f) continue; 

                // 判断 pointerPosition 是否在 graphic 的 Rectangle 区域内
                if (_CheckCamView(graphic, screenCenterPoint, eventCamera) && graphic.Raycast(screenCenterPoint, eventCamera) )
                    m_RaycastResults.Add(graphic);
            }

            //对所有射线检测成功的 graphics 按照深度 depth 从小到大排序。
            m_RaycastResults.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));

            int totalCount = m_RaycastResults.Count;
            for (var index = 0; index < totalCount; index++)
            {
                var graphic = m_RaycastResults[index].GetComponent<Graphic>();
                float dist;
                new Plane(graphic.transform.forward, graphic.transform.position).Raycast(ray, out dist);

                //判断在ui正前方多少距离内有效
                if (dist > (rayEnterDist + reversedRayDist)) continue;

                resultAppendList.Add(new RaycastResult
                {
                    gameObject = graphic.gameObject,
                    module = this,
                    distance = dist,
                    worldPosition = ray.GetPoint(dist),
                    worldNormal = -graphic.transform.forward,
                    screenPosition = screenCenterPoint,
                    index = resultAppendList.Count,
                    depth = graphic.depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder
                });
            }

            data.ignoreReversedGraphics = ignoreReversedGraphics;
        }

        private bool _CheckCamView(Graphic graphic, Vector3 screenCenterPoint, Camera camera)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenCenterPoint, camera);
        }

        protected override void OnDestroy()
        {
            if (fallbackCam != null)
            {
                GameObject.Destroy(fallbackCam.gameObject);
                fallbackCam = null;
            }
            base.OnDestroy();
        }
    }
}